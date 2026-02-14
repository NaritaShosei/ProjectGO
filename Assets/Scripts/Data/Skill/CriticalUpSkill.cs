using UnityEngine;

[CreateAssetMenu(fileName = "CriticalUpSkill", menuName = "GameData/Skill/CriticalUpSkill")]

public class CriticalUpSkill : SkillBase
{
    public override void OnAcquire(IStatUpgradable stats, int acquireCount)
    {
        if (acquireCount < 1 || acquireCount > _criticalUps.Length)
        {
            Debug.LogWarning($"CriticalUpSkill: acquireCount({acquireCount}) is out of range.");
            return;
        }

        stats.AddCriticalRate(_criticalUps[acquireCount - 1]);
    }

    public override bool CanAcquire(int acquireCount)
    {
        return acquireCount < _criticalUps.Length;
    }

    [SerializeField] private float[] _criticalUps; // acquireCountに応じたクリティカル率の上昇値 
}
