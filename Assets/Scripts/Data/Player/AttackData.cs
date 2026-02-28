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

    public bool EnableKnockback => _enableKnockback;
    public float KnockbackPower => _knockbackPower;
    public float KnockbackUpward => _knockbackUpward;

    public AttackMoveType MoveType => _moveType;
    public float MoveDistance => _moveDistance;
    public float MoveSpeed => _moveSpeed;
    public float MoveDuration => _moveDuration;
    public bool StopOnHit => _stopOnHit;
    public bool IsPhantom => _isPhantom;

    public HitStopData HitStopData => _hitStopData;

    [Header("Basic Info")]
    [SerializeField] private int _attackId;
    [SerializeField] private string _attackName;
    [SerializeField] private PlayerMode _mode;
    [SerializeField] private AttackType _attackType;
    [SerializeField] private int _comboIndex;
    [SerializeField] private ChargeLevel _requiredCharge;

    [Header("Damage")]
    [SerializeField] private float _damageMultiplier = 1;

    [Header("Range")]
    [SerializeField] private float _attackRange = 1;
    [SerializeField] private float _attackRadius = 1;

    [Header("Combo")]
    [SerializeField] private int _nextComboAttackId = -1;

    [Header("Knockback")]
    [SerializeField] private bool _enableKnockback = false;
    [SerializeField] private float _knockbackPower = 5f;
    [SerializeField] private float _knockbackUpward = 0f;

    [Header("Homing")]
    [SerializeField] private bool _enableHoming = false;
    [SerializeField] private float _homingRadius = 5f;
    [SerializeField] private float _homingAngle = 45f;
    [SerializeField] private float _homingStrength = 10f;

    [Header("Movement")]
    [SerializeField] private AttackMoveType _moveType = AttackMoveType.None;
    [SerializeField] private float _moveDistance = 0f;
    [SerializeField] private float _moveSpeed = 0f;
    [SerializeField] private float _moveDuration = 0f;
    [SerializeField] private bool _stopOnHit = true;
    [SerializeField] private bool _isPhantom = false; // すり抜け攻撃かどうか 

    [Header("Hit Stop")]
    [SerializeField] private HitStopData _hitStopData;
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
    Warrior,
    [InspectorName("雷神")]
    Thunder
}

public enum AttackMoveType
{
    [InspectorName("移動なし")]
    None,   // その場

    [InspectorName("突進")]
    Dash,   // 直線突進

    [InspectorName("ステップ")]
    Step,   // 小移動

    [InspectorName("曲線移動 / ホーミング")]
    Curve,  // 曲線 / ホーミング（将来）
}
