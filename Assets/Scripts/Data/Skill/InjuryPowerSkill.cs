using UnityEngine;

[CreateAssetMenu(fileName = "InjurypowerSkill", menuName = "GameData/Skill/InjuryPowerSkill")]

public class InjuryPowerSkill : SkillBase,IStatModifier
{
    public StatType TargetStat => StatType.Attack;

    public override void OnAcquire(IPlayerStats stats)
    {
        _playerStats = stats;
        stats.AddModifier(this);
    }

    public float Modify(float current)
    {
        if (_playerStats.CurrentHealth <= _playerStats.MaxHealth * _isInjuryPercent)
            return current * _attackStatusBonusPercent;
        else return current;
    }

    [Header("攻撃力上昇量")]
    [SerializeField] private float _attackStatusBonusPercent = 1.3f;
    [Header("攻撃力上昇割合")]
    [SerializeField] private float _isInjuryPercent = 0.5f;

    private IPlayerStats _playerStats;
}
