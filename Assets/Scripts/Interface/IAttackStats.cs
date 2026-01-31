public interface IAttackStats
{
    public float AttackPower { get; }
    public float CriticalRate { get; }
    public void ApplyEvolution(float attackPowerBonus, float criticalRateBonus);
}