using System;
using System.Collections.Generic;

/// <summary>
/// 同時攻撃可能数を管理するスロット
/// Bossは優先してスロットを確保できる
/// </summary>
public sealed class EnemyAttackerSlot : IEnemyAttackerSlot
{
    /// <summary>
    /// スロットが解放されたときに発火するイベント
    /// 待機中の敵がこのイベントを受けてTryAcquireを再試行する
    /// </summary>
    public event Action OnSlotReleased;

    public EnemyAttackerSlot(int maxSlots = 0)
    {
        _maxSlots = Math.Max(0, maxSlots);
    }

    /// <summary>
    /// スロット上限を更新する
    /// 敵の総数変化（スポーン・死亡）のたびに前衛数に合わせて呼ぶ
    /// </summary>
    public void UpdateMaxSlots(int newMax)
    {
        _maxSlots = Math.Max(0, newMax);
    }

    /// <summary>
    /// 指定したEnemyがスロットを確保済みかどうかを返す
    /// </summary>
    public bool IsAcquired(int enemyId)
    {
        return _holders.Contains(enemyId);
    }

    /// <summary>
    /// スロットの確保を試みる
    /// Boss以外はスロット上限を超えた場合はfalseを返す
    /// </summary>
    public bool TryAcquire(int enemyId, int slotCost)
    {
        slotCost = Math.Max(1, slotCost);

        if (_holders.Contains(enemyId)) return true;

        if (_usedSlots + slotCost > _maxSlots) return false;

        _holders.Add(enemyId);
        _usedSlots += slotCost;
        return true;
    }

    /// <summary>
    /// スロットを解放し、待機中の敵へOnSlotReleasedを発火する
    /// </summary>
    public void Release(int enemyId, int slotCost)
    {
        slotCost = Math.Max(1, slotCost);

        if (!_holders.Remove(enemyId)) return;

        _usedSlots -= slotCost;

        if (_usedSlots < 0) _usedSlots = 0;

        OnSlotReleased?.Invoke();
    }

    /// <summary>
    /// 全スロット状態をクリアする
    /// </summary>
    public void Reset()
    {
        _holders.Clear();
        _usedSlots = 0;
    }

    private int _maxSlots;
    private int _usedSlots;
    private readonly HashSet<int> _holders = new();
}
