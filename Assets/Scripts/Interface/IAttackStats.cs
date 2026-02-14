public interface IAttackStats
{
    public float AttackPower { get; }
    /// <summary> クリティカル率(0～1) 1を超えると確定クリティカル </summary>
    public float CriticalRate { get; }
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

public interface IStaminaStats
{
    float MaxStamina { get; }
    float CurrentStamina { get; }
}
