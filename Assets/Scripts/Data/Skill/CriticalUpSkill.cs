using UnityEngine;

[CreateAssetMenu(fileName = "CriticalUpSkill", menuName = "GameData/Skill/CriticalUpSkill")]

public class CriticalUpSkill : SkillBase
{
    public override void OnAcquire(IStatUpgradable stats, int acquireCount)
    {
        stats.AddCriticalRate(_criticalUps[acquireCount - 1]);
    }

    public override bool CanAcquire(int acquireCount)
    {
        return acquireCount < _criticalUps.Length;
    }

    [SerializeField] private float[] _criticalUps; // acquireCountに応じたクリティカル率の上昇値 
}
