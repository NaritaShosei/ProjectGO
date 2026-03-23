using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 前衛・後衛を管理するフォーメーションシステム
/// IEnemyAttackerSlotのファサードとして機能し、既存のBehaviourへの変更なしで前衛選出を制御する
///
/// 前衛選出ルール:
///   前衛数 = max(2, ceil(総敵数 × 0.3))
///   正面エリア（プレイヤー前方±backAttackAngle以内）のエントリをCombatPower降順で上位N体を前衛とする
///
/// 背後スロット譲渡ルール（Tick内で判定）:
///   前衛がプレイヤー背後に移動かつCoolDown中 → CP以下の正面非前衛へスロットを譲渡する
/// </summary>
public sealed class EnemyFormationSystem : IEnemyFormationSystem
{
    /// <summary>
    /// 内部スロットが解放されたときに発火するイベント
    /// </summary>
    public event Action OnSlotReleased;

    /// <summary>
    /// フォーメーションシステムを初期化する
    /// </summary>
    /// <param name="maxSlots">同時攻撃スロット上限数</param>
    /// <param name="playerTransform">プレイヤーの向き・位置参照（背後判定に使用）</param>
    /// <param name="backAttackAngle">背後エリアとみなす角度（プレイヤー正面からの閾値、デフォルト90°）</param>
    public EnemyFormationSystem(int maxSlots, Transform playerTransform, float backAttackAngle = 90f)
    {
        _innerSlot = new EnemyAttackerSlot(maxSlots);
        _innerSlot.OnSlotReleased += HandleInnerSlotReleased;
        _playerTransform = playerTransform;
        // dot積閾値: cos(backAttackAngle) より小さければ背後エリアとみなす
        _backDotThreshold = Mathf.Cos(backAttackAngle * Mathf.Deg2Rad);
    }

    // ─── IEnemyFormationSystem ───────────────────────────────────────────

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
    /// 毎フレーム呼び出す
    /// 背後に移動かつCoolDown中の前衛からスロットを正面の低CPエントリへ譲渡する
    /// </summary>
    public void Tick(float deltaTime)
    {
        if (_playerTransform == null) return;

        // イテレーション中の辞書変更を避けるため対象IDを事前に収集する
        List<int> transferTargets = null;
        foreach (var kvp in _entries)
        {
            var entry = kvp.Value;
            if (!entry.IsVanguard) continue;
            if (!IsInPlayerBack(entry.Enemy)) continue;
            if (!entry.Participant.IsInAttackCooldown) continue;

            transferTargets ??= new List<int>();
            transferTargets.Add(kvp.Key);
        }

        if (transferTargets == null) return;

        foreach (int id in transferTargets)
        {
            TransferSlotToFront(id);
        }
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
    private readonly Transform _playerTransform;

    // 背後判定のdot積閾値（cos(backAttackAngle)）
    private readonly float _backDotThreshold;

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
    /// 正面エリアのエントリからCombatPower降順で上位N体を前衛とし、
    /// 降格した前衛のスロットを強制解放する
    /// IsVanguardフラグを先に更新してからスロット解放することで、
    /// OnSlotReleased発火時に新前衛が正しくTryAcquireできるようにする
    /// </summary>
    private void ReevaluateFormation()
    {
        int total = _entries.Count;
        if (total == 0) return;

        // 前衛数: max(2, ceil(total × 0.3))
        int vanguardCount = Mathf.Max(2, Mathf.CeilToInt(total * 0.3f));

        // 正面・背後エントリを分類する
        var frontCandidates = new List<FormationEntry>(_entries.Count);
        var backEntries = new List<FormationEntry>();

        foreach (var kvp in _entries)
        {
            if (IsInPlayerBack(kvp.Value.Enemy))
                backEntries.Add(kvp.Value);
            else
                frontCandidates.Add(kvp.Value);
        }

        // 正面エントリをCombatPower降順にソート
        frontCandidates.Sort((a, b) => b.CombatPower.CompareTo(a.CombatPower));

        // IsVanguardフラグを先に全エントリに適用する
        for (int i = 0; i < frontCandidates.Count; i++)
        {
            frontCandidates[i].IsVanguard = i < vanguardCount;
        }
        foreach (var entry in backEntries)
        {
            entry.IsVanguard = false;
        }

        // 降格したエントリのスロットを解放する（OnSlotReleased発火は解放後）
        foreach (var entry in frontCandidates)
        {
            if (!entry.IsVanguard)
                DemoteIfAcquired(entry);
        }
        foreach (var entry in backEntries)
        {
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

    /// <summary>
    /// 背後に移動した前衛のスロットを正面の低CPエントリへ譲渡する
    /// IsVanguardを先に更新してからスロット解放することで、
    /// OnSlotReleased発火時に昇格候補が正しくTryAcquireできるようにする
    /// </summary>
    private void TransferSlotToFront(int fromId)
    {
        if (!_entries.TryGetValue(fromId, out var fromEntry)) return;

        // 正面かつ非前衛の中でCPが最大かつfromEntry以下の候補を選ぶ
        FormationEntry best = null;
        foreach (var kvp in _entries)
        {
            var other = kvp.Value;
            if (other.IsVanguard) continue;
            if (IsInPlayerBack(other.Enemy)) continue;
            if (other.CombatPower > fromEntry.CombatPower) continue;
            if (best == null || other.CombatPower > best.CombatPower)
                best = other;
        }

        if (best == null) return;

        // IsVanguardを先に更新してからスロット解放する
        fromEntry.IsVanguard = false;
        best.IsVanguard = true;
        // 解放によりOnSlotReleasedが発火し、bestがTryAcquireを試みる
        DemoteIfAcquired(fromEntry);
    }

    /// <summary>
    /// EnemyがプレイヤーのXZ平面上で背後エリアに位置するかを判定する
    /// dot積がthresholdを下回る（角度がbackAttackAngleを超える）場合に背後とみなす
    /// </summary>
    private bool IsInPlayerBack(IEnemy enemy)
    {
        if (_playerTransform == null) return false;

        Vector3 toEnemy = enemy.Position - _playerTransform.position;
        toEnemy.y = 0f;

        // 極めて近い場合は背後と判定しない
        if (toEnemy.sqrMagnitude < 0.001f) return false;

        Vector3 playerForward = _playerTransform.forward;
        playerForward.y = 0f;

        float dot = Vector3.Dot(playerForward.normalized, toEnemy.normalized);
        return dot < _backDotThreshold;
    }
}
