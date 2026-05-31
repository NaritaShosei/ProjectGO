using UnityEngine;

[CreateAssetMenu(fileName = "AbsorptionBlowSkill", menuName = "GameData/Skill/AbsorptionBlowSkill")]

public class AbsorptionBlowSkill : SkillBase
{
    public override void Apply(ref AttackContext context)
    {
        IPlayerStats cont = context.PlayerStats;
        float healMultiplier = context.AttackPower * _healingRate;

        context.OnHit += _ =>
            cont.Healing(healMultiplier);
    }

    [Header("HP回復割合")]
    [SerializeField] private float _healingRate = 0.2f;
}
