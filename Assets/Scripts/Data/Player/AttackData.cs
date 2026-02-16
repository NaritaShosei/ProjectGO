using UnityEngine;

[CreateAssetMenu(fileName = "AttackData", menuName = "GameData/AttackData")]

public class AttackData : ScriptableObject
{
    public int AttackId => _attackId;
    public string AttackName => _attackName;
    public PlayerMode Mode => _mode;
    public AttackType AttackType => _attackType;
    public int ComboIndex => _comboIndex;
    public ChargeLevel RequiredCharge => _requiredCharge;

    public float DamageMultiplier => _damageMultiplier;
    public float AttackRange => _attackRange;
    public float AttackRadius => _attackRadius;

    public int NextComboAttackId => _nextComboAttackId;

    public bool EnableHoming => _enableHoming;
    public float HomingRadius => _homingRadius;
    public float HomingAngle => _homingAngle;
    public float HomingStrength => _homingStrength;


    [SerializeField] private int _attackId;
    [SerializeField] private string _attackName;
    [SerializeField] private PlayerMode _mode;
    [SerializeField] private AttackType _attackType;
    [SerializeField] private int _comboIndex;              // コンボの何段目か（0始まり）
    [SerializeField] private ChargeLevel _requiredCharge;  // 必要なチャージレベル

    [SerializeField] private float _damageMultiplier = 1;
    [SerializeField] private float _attackRange = 1;
    [SerializeField] private float _attackRadius = 1;

    [SerializeField] private int _nextComboAttackId = -1;      // 次のコンボ攻撃ID

    [Header("Homing")]
    [SerializeField] private bool _enableHoming = false;
    [SerializeField] private float _homingRadius = 5f;
    [SerializeField] private float _homingAngle = 45f;
    [SerializeField] private float _homingStrength = 10f; // 値が大きいほど速く回転（5〜15推奨）

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
public enum PlayerMode
{
    [InspectorName("闘神")]
    Warrior,   // 闘神モード
    [InspectorName("雷神")]
    Thunder    // 雷神モード
}
