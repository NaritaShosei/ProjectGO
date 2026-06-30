using UnityEngine;

[CreateAssetMenu(fileName = "PlayerReviveSkill", menuName = "GameData/Skill/PlayerReviveSkill")]

public class PlayerReviveSkill : SkillBase
{
    public StatType TargetStat => StatType.Heal;

    public override void OnAcquire(IPlayerStats stats)
    {
        _playerStats = stats;
        stats.OnBeforeDead += PlayerRevive;
    }

    [SerializeField, Header("回復量")]
    private float _healAmount;

    private IPlayerStats _playerStats;
    private bool _isUseSkill = false;

    /// <summary>
    /// 使用していなければ一度だけ復活
    /// </summary>
    /// <returns></returns>
    private bool PlayerRevive()
    {
        if (_isUseSkill) return false;

        _playerStats.Healing(_healAmount);
        _isUseSkill = true;

        return true;
    }
}
