using UnityEngine;

[CreateAssetMenu(fileName = "InjuryDefenceSkill", menuName = "GameData/Skill/InjuryDefenceSkill")]

public class InjuryDefenceSkill : SkillBase,IStatModifier
{
    public StatType TargetStat => StatType.Defense;

    public override void OnAcquire(IPlayerStats stats)
    {
        _playerStats = stats;
        stats.AddModifier(this);
    }

    public float Modify(float current)
    {
        if(_playerStats.CurrentHealth <= (_playerStats.MaxHealth * _isInjuryPercent))
            return current * _defenceStatusBonusPercent;
        else
            return current;
    }

    [Header("防御力上昇率"),Min(0f)]
    [SerializeField] private float _defenceStatusBonusPercent = 1.3f;
    [Header("防御力上昇適応体力割合"),Range(0f,1f)]
    [SerializeField] private float _isInjuryPercent = 0.5f;

    private IPlayerStats _playerStats;
}
