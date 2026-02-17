using UnityEngine;

[CreateAssetMenu(fileName = "ConditionalAttackBoostSkill", menuName = "GameData/Skill/ConditionalAttackBoostSkill")]

public class ConditionalAttackBoostSkill : SkillBase
{
    public override bool CanApply(AttackContext context, AttackData data)
    {
        // 攻撃タイプが一致しているか
        bool isTargetAttackType = data.AttackType == _targetAttackType;

        // 必要なコンボ数に到達しているか
        bool hasRequiredComboCount = data.ComboIndex >= _requiredComboCount;

        // コンボの最終段かどうか
        bool isLastCombo = data.NextComboAttackId == -1;

        return isTargetAttackType
            && hasRequiredComboCount
            && isLastCombo;
    }

    public override void Apply(ref AttackContext context)
    {
        // 攻撃力を一定割合増加
        context.AttackPower *= (1f + _boostAmount);
    }

    [SerializeField] private float _boostAmount = 0.5f;
    [SerializeField] private int _requiredComboCount = 1;
    [SerializeField] private AttackType _targetAttackType = AttackType.LightAttack;
}
