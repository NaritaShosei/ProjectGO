using UnityEngine;

[CreateAssetMenu(menuName = "GameData/ModeData", fileName = "ModeData")]
public class PlayerModeData : ScriptableObject
{
    [Header("モード情報")]
    [SerializeField] private string _modeName;
    [SerializeField] private ModeType _modeType; // どのStateクラスを使うか指定
    [SerializeField] private AnimationClip _modeChangeClip;

    [Header("モードのキャラクターデータ")]
    [SerializeField] private CharacterData _characterData;

    [Header("攻撃データ")]
    [SerializeField, Tooltip("コンボの最初の攻撃")] private AttackData _firstComboData;
    [SerializeField, Tooltip("チャージしない攻撃")] private AttackData _heavyAttackData;
    [SerializeField, Tooltip("中チャージ攻撃")] private AttackData _chargedAttackData;
    [SerializeField, Tooltip("強チャージ攻撃")] private AttackData _superChargedAttackData;
    [SerializeField, Tooltip("強チャージ攻撃からの派生攻撃")] private AttackData _superChargedComboAttackData;
    [SerializeField, Tooltip("回避攻撃")] private AttackData _dodgeAttackData;

    [Header("モード固有データ")]
    [SerializeField] private AbilityDataBase _abilityData;

    public string ModeName => _modeName;
    public ModeType ModeType => _modeType;
    public AnimationClip ModeChangeClip => _modeChangeClip;
    public CharacterData CharacterData => _characterData;
    public AttackData FirstComboData => _firstComboData;
    public AttackData HeavyAttackData => _heavyAttackData;
    public AttackData ChargedAttackData => _chargedAttackData;
    public AttackData SuperChargedAttackData => _superChargedAttackData;
    public AttackData SuperChargedComboAttackData => _superChargedComboAttackData;
    public AttackData DodgeAttackData => _dodgeAttackData;

    public AbilityDataBase AbilityData => _abilityData;
}

public enum ModeType
{
    [InspectorName("戦神")]
    BattleGod,
    [InspectorName("雷神")]
    ThunderGod,
}