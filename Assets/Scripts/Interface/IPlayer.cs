using System;
using UnityEngine;

public interface IPlayer :
     IPlayerStats
{
    public event Action OnDead;
    public event Action<PlayerMode, ChargeLevel> OnAttackHit;
    public event Action<PlayerMode> OnModeChanged;
    public event Action OnDownRecoveryEnded;

    public bool IsDown { get; }

    public void StartDownRecovery();
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
    public event Action<float, float, float> OnThunderGaugeChanged;

    public event Action<Transform> OnEndDodge;

    /// <summary> ジャスト回避成立通知/// </summary>
    public event Action OnJustDodgeSuccess;

    /// <summary> 死亡直前イベント。true を返すと死亡をキャンセルする。 /// </summary>
    public event Func<bool> OnBeforeDead;
}


public interface IModifierHolder
{
    public void AddModifier(IStatModifier modifier);
    public void AddDamageReactionModifier(IDamageReactionModifier modifier);
    public void AddDamageModifier(IDamageModifier modifier);
}

/// <summary>
/// 現在のモードを公開するインターフェース。
/// </summary>
public interface IModeProvider
{
    public PlayerMode CurrentMode { get; }
}
