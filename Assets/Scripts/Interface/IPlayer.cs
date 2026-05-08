using System;
using UnityEngine;

public interface IPlayer :
     IPlayerStats
{
    /// <summary>
    /// ロックオンなどの中心のTransformを取得する
    /// </summary>
    public Transform GetTargetCenter();
}

public interface IPlayerStats :
    IHealth,
    IAttackStats,
    IDefenseStats,
    IHealthStats,
    IThunderGaugeStats,
    IBaseStats,
    IModifierHolder,
    IModeProvider
{
    /// <summary> 雷ゲージ変化通知 (current, max, initialMax) </summary>
    event Action<float, float, float> OnThunderGaugeChanged;
}


public interface IModifierHolder
{
    void AddModifier(IStatModifier modifier);
}

/// <summary>
/// 現在のモードを公開するインターフェース。
/// </summary>
public interface IModeProvider
{
    public PlayerMode CurrentMode { get; }
}
