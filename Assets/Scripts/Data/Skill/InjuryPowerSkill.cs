using UnityEngine;

[CreateAssetMenu(fileName = "InjurypowerSkill", menuName = "GameData/Skill/InjuryPowerSkill")]

public class InjuryPowerSkill : SkillBase
{
    public StatType TargetStat => StatType.Attack;

    public override void Apply(ref AttackContext context)
    {
        context.AttackPower *= _attackStatusBonusPercent;
    }

    public override bool CanApply(AttackContext context, AttackData data)
    {
        bool isInjury = context.PlayerStats.CurrentHealth <= context.PlayerStats.MaxHealth * _isInjuryPercent;

        return isInjury;
    }

    [Header("攻撃力上昇量")]
    [SerializeField] private float _attackStatusBonusPercent = 1.3f;
    [Header("攻撃力上昇割合")]
    [SerializeField] private float _isInjuryPercent = 0.5f;
}
