using UnityEngine;

[CreateAssetMenu(fileName = "PlayerReviveSkill", menuName = "GameData/Skill/PlayerReviveSkill")]

public class PlayerReviveSkill : SkillBase, IStatModifier
{
    public StatType TargetStat => StatType.Health;

    public override void OnAcquire(IPlayerStats stats)
    {
        _playerStats = stats;
        stats.AddModifier(this);
    }

    public float Modify(float current)
    {
        if (_playerStats.CurrentHealth <= 0)
            return current += _healAmount;
        else return current;
    }

    [SerializeField]
    private float _healAmount;

    private IPlayerStats _playerStats;
}
