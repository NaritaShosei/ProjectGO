/// <summary>
/// 攻撃力のステータスを提供するインターフェース。
/// </summary>
public interface IAttackStats
{
    float AttackPower { get; }
    /// <summary> クリティカル率(0～1) 1を超えると確定クリティカル </summary>
    float CriticalRate { get; }
}

/// <summary>
/// 防御力のステータスを提供するインターフェース。
/// </summary>
public interface IDefenseStats
{
    float DefensePower { get; }
}

/// <summary>
/// HPのステータスを提供するインターフェース。
/// </summary>
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
}

/// <summary>
/// 攻撃力、クリティカル率、防御力、HP、雷ゲージなどの基礎ステータスを提供するインターフェース。
/// </summary>
public interface IBaseStats
{
    float BaseAttackPower { get; }
    float BaseCriticalRate { get; }
    float BaseDefensePower { get; }
    float BaseMaxHealth { get; }
    float BaseMaxThunderGauge { get; }
}
