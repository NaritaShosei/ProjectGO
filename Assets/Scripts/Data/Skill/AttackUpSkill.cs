using UnityEngine;

[CreateAssetMenu(fileName = "AttackUpSkill", menuName = "GameData/Skill/AttackUpSkill")]
public class AttackUpSkill : SkillBase
{
    public override bool CanAcquire(int acquireCount)
    {
        return acquireCount < _attackUps.Length;
    }

    public override void OnAcquire(IPlayerStats stats, int acquireCount)
    {
        if (acquireCount < 1 || acquireCount > _attackUps.Length)
        {
            Debug.LogWarning($"AttackUpSkill: acquireCount({acquireCount}) is out of range.");
            return;
        }

        stats.AddAttackPower(_attackUps[acquireCount - 1]);
        Debug.Log(stats.AttackPower);
    }
    [SerializeField] private float[] _attackUps;
}
