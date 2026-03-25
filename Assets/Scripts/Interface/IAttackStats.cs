public interface IAttackStats
{
    float AttackPower { get; }
    /// <summary> クリティカル率(0～1) 1を超えると確定クリティカル </summary>
    float CriticalRate { get; }
}

public interface IDefenseStats
{
    float DefensePower { get; }
}

public interface IHealthStats
{
    float MaxHealth { get; }
    float CurrentHealth { get; }
}

/// <summary>
/// 旧 IStaminaStats に相当する雷ゲージの読み取り専用インターフェース。
/// UIや外部から現在値・最大値を参照する際に使用する。
/// </summary>
public interface IThunderGaugeStats
{
    float MaxThunderGauge { get; }
    float CurrentThunderGauge { get; }
    float InitialMaxThunderGauge { get; }
}
