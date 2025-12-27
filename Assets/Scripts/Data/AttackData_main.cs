using UnityEngine;

[CreateAssetMenu(fileName = "AttackData", menuName = "GameData/AttackData")]

public class AttackData_main : ScriptableObject
{
    public int AttackId => _attackId;
    public string AttackName => _attackName;
    public CombatMode Mode => _mode;
    public AttackType AttackType => _attackType;
    public int ComboIndex => _comboIndex;
    public ChargeLevel RequiredCharge => _requiredCharge;

    public float Damage => _damage;
    public float AttackRange => _attackRange;
    public string AnimationName => _animationName;
    public float AnimationDuration => _animationDuration;

    public float ComboWindowStart => _comboWindowStart;
    public float ComboWindowEnd => _comboWindowEnd;
    public int NextComboAttackId => _nextComboAttackId;

    [SerializeField] private int _attackId;
    [SerializeField] private string _attackName;
    [SerializeField] private CombatMode _mode;
    [SerializeField] private AttackType _attackType;
    [SerializeField] private int _comboIndex;              // コンボの何段目か（0始まり）
    [SerializeField] private ChargeLevel _requiredCharge;  // 必要なチャージレベル

    [SerializeField] private float _damage;
    [SerializeField] private float _attackRange;
    [SerializeField] private string _animationName;
    [SerializeField] private float _animationDuration;

    [SerializeField] private float _comboWindowStart;     // コンボ受付開始時間
    [SerializeField] private float _comboWindowEnd;       // コンボ受付終了時間
    [SerializeField] private int _nextComboAttackId;      // 次のコンボ攻撃ID
}

// 攻撃の段階（チャージレベル）
public enum ChargeLevel
{
    [InspectorName("溜めなし")]
    None = 0,
    [InspectorName("溜め1")]
    Level1 = 1,
    [InspectorName("溜め2")]
    Level2 = 2
}

// 攻撃タイプ
public enum AttackType
{
    [InspectorName("弱攻撃")]
    LightAttack,
    [InspectorName("強攻撃")]
    HeavyAttack,
    [InspectorName("回避攻撃")]
    DodgeAttack
}

// モード
public enum CombatMode
{
    [InspectorName("闘神")]
    Warrior,   // 闘神モード
    [InspectorName("雷神")]
    Thunder    // 雷神モード
}