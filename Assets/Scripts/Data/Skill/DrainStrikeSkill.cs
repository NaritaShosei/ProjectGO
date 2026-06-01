using UnityEngine;

[CreateAssetMenu(fileName = "DrainStrikeSkill", menuName = "GameData/Skill/DrainStrikeSkill")]

public class DrainStrikeSkill : SkillBase
{
    public override void Apply(ref AttackContext context)
    {
        IPlayerStats stats = context.PlayerStats;
        float healMultiplier = context.AttackPower * _healingRate;

        context.OnHit += _ =>
            stats.Healing(healMultiplier);
    }

    [Header("HP回復割合")]
    [SerializeField] private float _healingRate = 0.2f;
}
