using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

/// <summary>
/// 前衛・後衛を管理するフォーメーションシステム
/// IEnemyAttackerSlotのファサードとして機能し、既存のBehaviourへの変更なしで前衛選出を制御する
///
/// 前衛選出ルール:
///   前衛数 = max(2, ceil(総敵数 × 0.3))
///   全エントリをCombatPower降順でソートし上位N体を前衛とする（位置は考慮しない）
///
/// アタッカー入れ替えトリガー:
///   ① 死亡: OnDeadハンドラ → ReevaluateFormation()
///   ② 強敵スポーン: Register() → ReevaluateFormation()
///   ③ 被弾: NotifyHit() → CP同等以下の前衛と入れ替え
///
/// 背後攻撃抑制:
///   MeleeAttackBehaviour.CanEnter()内でDistanceProfileのパラメータに基づき確率的に抑制する
/// </summary>
public sealed class EnemyFormationSystem : IEnemyFormationSystem
{
    /// <summary>
    /// 内部スロットが解放されたときに発火するイベント
    /// </summary>
    public event Action OnSlotReleased;

    /// <summary>
    /// フォーメーションシステムを初期化する
    /// スロット上限は敵登録・死亡のたびに ReevaluateFormation() が自動で更新する
    /// </summary>
    public EnemyFormationSystem()
    {
        _innerSlot = new EnemyAttackerSlot();
        _innerSlot.OnSlotReleased += HandleInnerSlotReleased;
    }

    // ─── IEnemyFormationSystem / IEnemyAttackerSlot ──────────────────────

    /// <summary>
    /// EnemyをFormationSystemに登録してフォーメーションを再評価する
    /// </summary>
    public void Register(IEnemy enemy, IFormationParticipant participant)
    {
        if (enemy == null || participant == null) return;

        int id = participant.EnemyId;
        if (_entries.ContainsKey(id)) return;

        var entry = new FormationEntry
        {
            Enemy = enemy,
            Participant = participant,
            CombatPower = participant.CombatPower,
            SlotCost = participant.FormationSlotCost,
            IsVanguard = false
        };

        // クロージャでIDをキャプチャすることで死亡時に確実にエントリを削除する
        Action<IEnemy> deadHandler = null;
        deadHandler = _ =>
        {
            entry.Enemy.OnDead -= deadHandler;

            _waitingGroupIds.Remove(id);
            _forcedVanguardIds.Remove(id);

            _entries.Remove(id);
            ReevaluateFormation();
        };
        entry.DeadHandler = deadHandler;
        enemy.OnDead += deadHandler;

        _entries[id] = entry;

        ReevaluateFormation();
    }

    /// <summary>
    /// EnemyGroupを登録してフォーメーションを再度決める
    /// </summary>
    public void RegisterWaitingGroup(EnemyGroup group)
    {
        if (group == null) return;

        foreach (IEnemy enemy in group.Members)
        {
            if (enemy == null) continue;
            if (!_entries.ContainsKey(enemy.Id)) continue;

            _forcedVanguardIds.Remove(enemy.Id);
            _waitingGroupIds.Add(enemy.Id);
        }

        ReevaluateFormation();
    }

    /// <summary>
    /// EnemyGroupが前衛に行くことが可能かどうかを判定する
    /// </summary>
    /// <param name="group"></param>
    /// <returns></returns>
    public bool CanPromoteGroup(EnemyGroup group)
    {
        if (group == null) return false;
        if (group.Phase != EnemyGroupPhase.Waiting) return false;
        if (group.Leader == null) return false;

        if (!_entries.TryGetValue(
                group.Leader.Id,
                out FormationEntry leaderEntry))
        {
            return false;
        }

        var groupIds = new HashSet<int>();

        foreach (IEnemy member in group.Members)
        {
            if (member != null)
            {
                groupIds.Add(member.Id);
            }
        }

        foreach (FormationEntry entry in _entries.Values)
        {
            // 同じグループのメンバーは比較対象外
            if (groupIds.Contains(entry.Participant.EnemyId))
                continue;

            // 前衛ではない敵も比較対象外
            if (!entry.IsVanguard)
                continue;

            // リーダーより戦闘力が高い前衛がいる
            if (entry.CombatPower > leaderEntry.CombatPower)
                return false;
        }

        return true;
    }

    /// <summary>
    /// EnemyGroupを前衛にさせる
    /// </summary>
    /// <param name="group"></param>
    /// <returns></returns>
    public bool TryPromoteGroup(
        EnemyGroup group)
    {
        if (!CanPromoteGroup(group))
            return false;

        foreach (IEnemy enemy in group.Members)
        {
            if (enemy == null)
                continue;

            if (!_entries.TryGetValue(
                    enemy.Id,
                    out FormationEntry entry))
            {
                continue;
            }

            _waitingGroupIds.Remove(enemy.Id);
            _forcedVanguardIds.Add(enemy.Id);
        }

        group.ReleaseFormation();

        ReevaluateFormation();

        // 5体全員にスロットを直接取得させる
        foreach (IEnemy enemy in group.Members)
        {
            if (enemy == null)
                continue;

            if (!_entries.TryGetValue(
                    enemy.Id,
                    out FormationEntry entry))
            {
                continue;
            }

            _innerSlot.TryAcquire(
                enemy.Id,
                entry.SlotCost);
        }

        OnSlotReleased?.Invoke();

        return true;
    }

    /// <summary>
    /// 後衛Enemyが被弾したことを通知する
    /// CP同等以下の前衛の中で最もCPが低い敵と入れ替える
    /// IsVanguardを先に更新してからスロット解放することで、
    /// OnSlotReleased発火時に昇格候補が正しくTryAcquireできるようにする
    /// </summary>
    public void NotifyHit(int enemyId)
    {
        if (!_entries.TryGetValue(enemyId, out var hitEntry)) return;
        // 待機中のグループは、被弾しても前衛へ昇格させない
        if (_waitingGroupIds.Contains(enemyId))
        {
            return;
        }
        // すでに前衛なら何もしない
        if (hitEntry.IsVanguard) return;

        // CP同等以下の前衛の中で最もCPが低い敵を降格候補とする
        FormationEntry worst = null;
        foreach (var kvp in _entries)
        {
            var entry = kvp.Value;
            int entryId = entry.Participant.EnemyId;
            if (!entry.IsVanguard) continue;
            if (_forcedVanguardIds.Contains(entryId)) continue;
            if (entry.CombatPower > hitEntry.CombatPower) continue;
            if (worst == null || entry.CombatPower < worst.CombatPower)
                worst = entry;
        }

        if (worst == null) return;

        // IsVanguardを先に更新してからスロット解放する
        worst.IsVanguard = false;
        hitEntry.IsVanguard = true;

        bool worstHadSlot = _innerSlot.IsAcquired(worst.Participant.EnemyId);
        DemoteIfAcquired(worst);

        // worstがスロット未保持だった場合は解放イベントが発火しないため明示的に通知する
        if (!worstHadSlot)
            OnSlotReleased?.Invoke();
    }

    // ─── IEnemyAttackerSlot ──────────────────────────────────────────────

    public bool IsAcquired(int enemyId) => _innerSlot.IsAcquired(enemyId);

    /// <summary>
    /// スロットの確保を試みる
    /// </summary>
    public bool TryAcquire(int enemyId, int slotCost)
    {
        // 前衛として登録されていない場合は取得不可
        if (!_entries.TryGetValue(enemyId, out var entry) || !entry.IsVanguard)
        {
            return false;
        }

        return _innerSlot.TryAcquire(enemyId, slotCost);
    }

    public void Release(int enemyId, int slotCost)
    {
        _innerSlot.Release(enemyId, slotCost);
    }

    /// <summary>
    /// 全エントリとスロット状態をクリアする
    /// シーン再ロードやゲームリセット時の使用を想定
    /// </summary>
    public void Reset()
    {
        _innerSlot.Reset();
        foreach (var kvp in _entries)
        {
            var entry = kvp.Value;
            if (entry.DeadHandler != null)
                entry.Enemy.OnDead -= entry.DeadHandler;
        }
        _waitingGroupIds.Clear();
        _forcedVanguardIds.Clear();
        _entries.Clear();
    }

    // ─── Private ─────────────────────────────────────────────────────────

    private readonly EnemyAttackerSlot _innerSlot;

    /// <summary>
    /// リーダーによって前衛かどうかが決定されるグループのID集合
    /// </summary>
    private readonly HashSet<int> _waitingGroupIds = new();
    private readonly HashSet<int> _forcedVanguardIds = new();

    // EnemyId → FormationEntry
    private readonly Dictionary<int, FormationEntry> _entries = new();

    private class FormationEntry
    {
        public IEnemy Enemy;
        public IFormationParticipant Participant;
        public float CombatPower;
        public int SlotCost;
        public bool IsVanguard;
        // 死亡時の購読解除に使用する
        public Action<IEnemy> DeadHandler;
    }

    private void HandleInnerSlotReleased()
    {
        OnSlotReleased?.Invoke();
    }

private void ReevaluateFormation()
{
    int total = _entries.Count;

    if (total == 0)
    {
        _innerSlot.UpdateMaxSlots(0);
        return;
    }

    int normalVanguardCount =
        Mathf.Min(
            total,
            Mathf.Max(
                2,
                Mathf.CeilToInt(total * 0.3f)));

    var allEntries =
        new List<FormationEntry>(_entries.Values);

    allEntries.Sort(
        (a, b) =>
            b.CombatPower.CompareTo(a.CombatPower));

    // 最初に全員を後衛に戻す
    foreach (FormationEntry entry in allEntries)
    {
        entry.IsVanguard = false;
    }

    int forcedCount = 0;

    // 強制前衛を先に選ぶ
    foreach (FormationEntry entry in allEntries)
    {
        int id = entry.Participant.EnemyId;

        if (_forcedVanguardIds.Contains(id))
        {
            entry.IsVanguard = true;
            forcedCount++;
        }
    }

        // 強制前衛に合わせて前衛数を決定する
        int targetVanguardCount =
        Mathf.Max(
            normalVanguardCount,
            forcedCount);

    int selectedCount = forcedCount;

    // 残りの枠を通常の戦闘力順で選ぶ
    foreach (FormationEntry entry in allEntries)
    {
        if (selectedCount >= targetVanguardCount)
            break;

        int id = entry.Participant.EnemyId;

        // 待機グループは前衛にしない
        if (_waitingGroupIds.Contains(id))
            continue;

        // すでに強制前衛として選択済み
        if (entry.IsVanguard)
            continue;

        entry.IsVanguard = true;
        selectedCount++;
    }

    // 前衛全員が取得できるだけのスロット数
    int requiredSlots = 0;

    foreach (FormationEntry entry in allEntries)
    {
        if (entry.IsVanguard)
        {
            requiredSlots +=
                Mathf.Max(1, entry.SlotCost);
        }
    }

    _innerSlot.UpdateMaxSlots(requiredSlots);

    // 後衛になった敵が持っているスロットを解放
    foreach (FormationEntry entry in allEntries)
    {
        if (!entry.IsVanguard)
        {
            DemoteIfAcquired(entry);
        }
    }
}

    /// <summary>
    /// スロットを保有している場合に強制解放する
    /// </summary>
    private void DemoteIfAcquired(FormationEntry entry)
    {
        int id = entry.Participant.EnemyId;
        if (_innerSlot.IsAcquired(id))
        {
            _innerSlot.Release(id, entry.SlotCost);
        }
    }

}
