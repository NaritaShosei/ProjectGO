using System;
using System.Collections.Generic;
using UnityEngine;

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
            _entries.Remove(id);
            ReevaluateFormation();
        };
        entry.DeadHandler = deadHandler;
        enemy.OnDead += deadHandler;

        _entries[id] = entry;

        ReevaluateFormation();
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
        // すでに前衛なら何もしない
        if (hitEntry.IsVanguard) return;

        // CP同等以下の前衛の中で最もCPが低い敵を降格候補とする
        FormationEntry worst = null;
        foreach (var kvp in _entries)
        {
            var entry = kvp.Value;
            if (!entry.IsVanguard) continue;
            if (entry.CombatPower > hitEntry.CombatPower) continue;
            if (worst == null || entry.CombatPower < worst.CombatPower)
                worst = entry;
        }

        if (worst == null) return;

        // IsVanguardを先に更新してからスロット解放する
        worst.IsVanguard = false;
        hitEntry.IsVanguard = true;
        // 解放によりOnSlotReleasedが発火し、hitEntryがTryAcquireを試みる
        DemoteIfAcquired(worst);
    }

    // ─── IEnemyAttackerSlot ──────────────────────────────────────────────

    public bool IsAcquired(int enemyId) => _innerSlot.IsAcquired(enemyId);

    /// <summary>
    /// スロットの確保を試みる
    /// ボス以外は前衛として登録済みの場合のみ取得可能
    /// </summary>
    public bool TryAcquire(int enemyId, int slotCost, bool isBoss)
    {
        if (!isBoss)
        {
            // 前衛として登録されていない場合は取得不可
            if (!_entries.TryGetValue(enemyId, out var entry) || !entry.IsVanguard)
                return false;
        }
        return _innerSlot.TryAcquire(enemyId, slotCost, isBoss);
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
        _entries.Clear();
    }

    // ─── Private ─────────────────────────────────────────────────────────

    private readonly EnemyAttackerSlot _innerSlot;

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

    /// <summary>
    /// 全エントリを対象に前衛を再選出する
    /// 全エントリをCombatPower降順でソートし上位N体を前衛とする（位置は考慮しない）
    /// IsVanguardフラグを先に更新してからスロット解放することで、
    /// OnSlotReleased発火時に新前衛が正しくTryAcquireできるようにする
    /// </summary>
    private void ReevaluateFormation()
    {
        int total = _entries.Count;
        if (total == 0) return;

        // 前衛数: max(2, ceil(total × 0.3))
        int vanguardCount = Mathf.Max(2, Mathf.CeilToInt(total * 0.3f));

        // スロット上限を前衛数に合わせて更新する
        _innerSlot.UpdateMaxSlots(vanguardCount);

        // 全エントリをCombatPower降順にソート
        var allEntries = new List<FormationEntry>(_entries.Values);
        allEntries.Sort((a, b) => b.CombatPower.CompareTo(a.CombatPower));

        // IsVanguardフラグを先に全エントリに適用する
        for (int i = 0; i < allEntries.Count; i++)
        {
            allEntries[i].IsVanguard = i < vanguardCount;
        }

        // 降格したエントリのスロットを解放する（OnSlotReleased発火は解放後）
        foreach (var entry in allEntries)
        {
            if (!entry.IsVanguard)
                DemoteIfAcquired(entry);
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
