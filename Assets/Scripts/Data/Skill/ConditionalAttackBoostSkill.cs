using UnityEngine;

[CreateAssetMenu(fileName = "ConditionalAttackBoostSkill", menuName = "GameData/Skill/ConditionalAttackBoostSkill")]
public class ConditionalAttackBoostSkill : GroundCrush
{
    public override void Apply(ref AttackContext context)
    {
        context.AttackPower *= 1f + _boostAmount;

        base.Apply(ref context);
    }

    [Header("派生設定")]
    [SerializeField] private float _boostAmount = 0.5f;
}
