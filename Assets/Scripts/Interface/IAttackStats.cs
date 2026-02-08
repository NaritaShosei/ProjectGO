public interface IAttackStats
{
    public float AttackPower { get; }
    /// <summary> クリティカル率(0～1) 1を超えると確定クリティカル </summary>
    public float CriticalRate { get; }
    /// <summary> 足し算で攻撃力やクリティカルを増加 </summary>
    public void ApplyEvolution(float attackPowerBonus, float criticalRateBonus);
}
