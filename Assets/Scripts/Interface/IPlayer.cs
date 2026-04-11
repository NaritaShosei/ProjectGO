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
    IStatUpgradable
{
    /// <summary> 雷ゲージ変化通知 (current, max, initialMax) </summary>
    event Action<float, float, float> OnThunderGaugeChanged;
}
