using UnityEngine;

public enum StatSkillType
{
    [InspectorName("体力")]
    HP,
    [InspectorName("攻撃力")]
    Attack,
    [InspectorName("防御力")]
    Defense,
    [InspectorName("クリティカル率")]
    Critical,
    [InspectorName("雷ゲージ")]
    Thunder
}


/// <summary>
/// パラメーター増加スキルの定義。
/// 基礎値 × ランダム割合で上昇量を決定する。
/// </summary>
[CreateAssetMenu(fileName = "StatSkillData", menuName = "GameData/StatSkill/StatSkillData")]
public class StatSkillData : ScriptableObject
{
    public StatSkillType StatType => _statType;
    public string DisplayName => _displayName;

    /// <summary>
    /// baseValue × ランダム割合 の上昇量を返す
    /// </summary>
    public float CalculateAmount(float baseValue)
    {
        float ratio = Random.Range(_minRatio, _maxRatio);
        return baseValue * ratio;
    }

    [SerializeField] private StatSkillType _statType;
    [SerializeField] private string _displayName;

    [Header("上昇割合（基礎値に対する割合）")]
    [Range(0f, 2f)]
    [SerializeField] private float _minRatio = 0.25f;

    [Range(0f, 2f)]
    [SerializeField] private float _maxRatio = 0.50f;
}
