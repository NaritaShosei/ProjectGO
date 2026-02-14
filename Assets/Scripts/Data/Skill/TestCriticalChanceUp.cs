using UnityEngine;

public class TestCriticalChanceUp : SkillBase
{
    public override void OnAcquire(IAttackStats stats, int acquireCount)
    {
        _upCriticalRate += stats.CriticalRate;
    }

    public override void Apply(ref AttackContext context)
    {
        context.CriticalChanceUp.UpCriticalRate += _upCriticalRate;
        context.CriticalChanceUp.DurationEffect += _durationEffect;
    }

    [SerializeField] private float _durationEffect;
    [SerializeField] private float _upCriticalRate;
}

public struct CriticalChanceUp
{
    public float DurationEffect;
    public float UpCriticalRate;
}
