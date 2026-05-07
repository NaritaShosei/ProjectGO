public interface IStatModifier
{
    StatType TargetStat { get; }

    float Modify(float current);
}

public enum StatType
{
    Health,
    Attack,
    Defense,
    CriticalRate,
    ThunderDrain,
    ThunderRecover,
    ThunderGauge
}
