using UnityEngine;

[CreateAssetMenu(fileName = "TestElectricSkill", menuName = "GameData/Skill/TestElectricSkill")]

public class TestElectricShockSkill : SkillBase
{
    public override void Apply(ref AttackContext context)
    {
        context.ElectricShock.GrantEffectProbability += _grantEffectProbability;
        context.ElectricShock.DurationEffect += _durationEffect;
        context.ElectricShock.UpDamagePercentage += _upDamagePercentage;
    }

    public override bool CanApply(AttackContext context, AttackData data)
    {
        return context.PlayerMode == PlayerMode.Thunder;
    }

    [SerializeField] private float _grantEffectProbability;              //状態異常付与確率
    [SerializeField] private float _durationEffect;                      //状態異常持続時間
    [SerializeField] private float _upDamagePercentage;                  //ダメージ上昇率
}

/// <summary>
/// Enemyに状態異常(感電)を付与する際に扱う情報
/// </summary>
public struct ElectricShock
{
    /// <summary>
    /// 状態異常を付与する確率。0.2なら20%の確率で付与される。
    /// </summary>
    public float GrantEffectProbability;
    /// <summary>
    /// 状態異常の持続時間。単位は秒。
    /// </summary>
    public float DurationEffect;
    /// <summary>
    /// 状態異常中のダメージ上昇率。0.5なら50%ダメージが上昇する。
    /// </summary>
    public float UpDamagePercentage;
}
