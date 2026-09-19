using UnityEngine;
using UnityEngine.Serialization;

public enum StatSkillType
{
    [InspectorName("体力")]
    HP,
    [InspectorName("攻撃力")]
    Attack,
    [InspectorName("クリティカル率")]
    Critical,
    [InspectorName("雷ゲージ")]
    Thunder
}


/// <summary>
/// パラメーター増加スキルの定義。
/// 基礎値 × 割合で上昇量を決定する。
/// </summary>
[CreateAssetMenu(fileName = "StatSkillData", menuName = "GameData/StatSkill/StatSkillData")]
public class StatSkillData : ScriptableObject
{
    public StatSkillType StatType => _statType;
    public string DisplayName => _displayName;

    /// <summary>
    /// baseValue × 割合 の上昇量を返す
    /// </summary>
    public float CalculateAmount(float baseValue)
    {
        return baseValue * _ratio;
    }

    [SerializeField] private StatSkillType _statType;
    [SerializeField] private string _displayName;

    [Header("上昇割合（基礎値に対する割合）")]

    [Range(0f, 2f)]
    [FormerlySerializedAs("_maxRatio")]
    [SerializeField] private float _ratio = 0.50f;
}
