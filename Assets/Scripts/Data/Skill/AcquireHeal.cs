using UnityEngine;

[CreateAssetMenu(fileName = "AcquireHealSkill", menuName = "GameData/Skill/AcquireHealSkill")]

public class AcquireHeal : SkillBase
{
    public override void OnAcquire(IPlayerStats stats)
    {
        float healAmount = stats.MaxHealth - stats.CurrentHealth;

        stats.Healing(healAmount);
    }
}
