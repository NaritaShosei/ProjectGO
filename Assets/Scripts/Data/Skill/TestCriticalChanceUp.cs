using UnityEngine;

public class TestCriticalChanceUp : SkillBase
{
    public override void OnAcquire(IAttackStats stats, int acquireCount)
    {
        _upCiriticalRate += stats.CriticalRate;
    }

    public override void Apply(ref AttackContext context)
    {
        context.CriticalChanceUp.UpCriticalRate += _upCiriticalRate;
        context.CriticalChanceUp.DurationEffect += _durationEffect;
    }

    [SerializeField] private float _durationEffect;
    [SerializeField] private float _upCiriticalRate;
}

public struct CriticalChanceUp
{
    public float DurationEffect;
    public float UpCriticalRate;
}
