using UnityEngine;

[CreateAssetMenu(fileName = "EvadeEnhancementSkill", menuName = "GameData/Skill/EvadeEnhancementSkill")]

public class EvadeEnhancementSkill : SkillBase, IStatModifier
{
    public StatType TargetStat => StatType.DodgeInvincibleTime;

    public override void OnAcquire(IPlayerStats stats)
    {
        stats.AddModifier(this);
    }

    public float Modify(float current)
    {
        Debug.Log($"回避の無敵時間を{current}から{current + dodgeInvincibleTimeBonus}に変更");

        return current + dodgeInvincibleTimeBonus;
    }

    [Header("スキル固有設定")]
    [SerializeField, Min(0f)] private float dodgeInvincibleTimeBonus = 0.15f;
}
