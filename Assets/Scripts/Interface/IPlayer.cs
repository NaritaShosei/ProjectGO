using System;

public interface IPlayer :
    ICharacter, IPlayerStats
{
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
