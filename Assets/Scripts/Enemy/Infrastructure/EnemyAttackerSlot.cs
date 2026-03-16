using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 同時攻撃可能数を管理するスロット
/// Bossは優先してスロットを確保できる
/// </summary>
public class EnemyAttackerSlot : IEnemyAttackerSlot
{
    // [追加] スロットが解放されたときに発火するイベント
    // 待機中の敵がこのイベントを受けてTryAcquireを再試行する
    public event Action OnSlotReleased;

    public EnemyAttackerSlot(int maxSlots)
    {
        _maxSlots = Math.Max(1, maxSlots);
    }

    public bool IsAcquired(int enemyId)
    {
        // Holdersに含まれていればスロット確保済み
        return _holders.Contains(enemyId);
    }

    public bool TryAcquire(int enemyId, int slotCost, bool isBoss)
    {
        slotCost = Math.Max(1, slotCost);

        // すでに確保済みならtrueを返す
        if (_holders.Contains(enemyId)) return true;

        // Boss以外はスロット上限チェック
        if (_usedSlots + slotCost > _maxSlots && !isBoss) return false;

        _holders.Add(enemyId);
        _usedSlots += slotCost;
        Debug.Log($"{enemyId}が攻撃枠を取得した");
        return true;
    }

    public void Release(int enemyId, int slotCost)
    {
        slotCost = Math.Max(1, slotCost);

        // 保持していなければ何もしない
        if (!_holders.Remove(enemyId)) return;

        _usedSlots -= slotCost;

        // 安全のため0未満にならないようにする
        if (_usedSlots < 0) _usedSlots = 0;

        // 待機中の敵にスロット解放を通知する
        OnSlotReleased?.Invoke();
    }

    public bool IsFull(int slotCost)
    {
        slotCost = Math.Max(1, slotCost);

        // 指定コスト分のスロットが残っていない場合は満杯とみなす
        return _usedSlots + slotCost > _maxSlots;
    }

    public void Reset()
    {
        _holders.Clear();
        _usedSlots = 0;
    }

    private readonly int _maxSlots;
    private int _usedSlots;
    private readonly HashSet<int> _holders = new();
}
