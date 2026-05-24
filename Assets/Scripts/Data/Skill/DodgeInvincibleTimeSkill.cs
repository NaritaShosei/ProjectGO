using UnityEngine;

[CreateAssetMenu(fileName = "DodgeInvincibleTimeSkillSkill", menuName = "GameData/Skill/DodgeInvincibleTimeSkill")]

public class DodgeInvincibleTimeSkill : SkillBase, IStatModifier
{
    public StatType TargetStat => StatType.DodgeInvincibleTime;

    public override void OnAcquire(IPlayerStats stats)
    {
        stats.AddModifier(this);
    }

    public float Modify(float current)
    {
        Debug.Log($"回避の無敵時間を{current}から{current + _dodgeInvincibleTimeBonus}に変更");

        return current + _dodgeInvincibleTimeBonus;
    }

    [Header("追加無敵時間")]
    [SerializeField, Min(0f)] private float _dodgeInvincibleTimeBonus = 0.15f;
}
