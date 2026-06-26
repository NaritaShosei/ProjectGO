
using UnityEngine;

/// <summary>
/// 闘神モード中、防御力を倍率分上昇させるパッシブスキル。
/// 獲得時にIStatModifierとしてPlayerStatsへ登録される。
/// </summary>
[CreateAssetMenu(fileName = "WarriorDefenseBoostSkill", menuName = "GameData/Skill/WarriorDefenseBoostSkill")]
public class WarriorDefenseBoostSkill : SkillBase, IStatModifier
{
    public StatType TargetStat => StatType.Defense;

    public override void OnAcquire(IPlayerStats stats)
    {
        // Modify内でCurrentModeを参照するために保持しておく
        _modeProvider = stats;
        stats.AddModifier(this);
    }

    /// <summary>
    /// 闘神モード中のみ防御力を倍率分上昇させる。
    /// それ以外のモードでは何もしない。
    /// </summary>
    public float Modify(float current)
    {
        if (_modeProvider == null) return current;
        if (_modeProvider.CurrentMode != PlayerMode.Warrior) return current;

        return current * _defenseMultiplier;
    }

    [Header("闘神モード中の防御力倍率")]
    [Tooltip("1.3 = 防御力が1.3倍になる")]
    [SerializeField, Min(0f)] private float _defenseMultiplier = 1.3f;

    // OnAcquire時に受け取ったIPlayerStats(=IModeProviderでもある)の参照
    private IModeProvider _modeProvider;
}
