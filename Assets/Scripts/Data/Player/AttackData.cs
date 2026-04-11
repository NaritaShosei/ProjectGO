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

    public string AnimationStateName => _animationStateName;
    public float TransitionDuration => _transitionDuration;

    [Header("Basic Info")]
    [SerializeField] private int _attackId; // 攻撃ID
    [SerializeField] private string _attackName; // 攻撃名
    [SerializeField] private PlayerMode _mode; // 闘神 or 雷神
    [SerializeField] private AttackType _attackType; // 攻撃タイプ（弱攻撃、強攻撃、回避攻撃）
    [SerializeField] private int _comboIndex; // コンボの何段目か（1スタート）。単発攻撃の場合は1。コンボ未対応の場合は-1。
    [SerializeField] private ChargeLevel _requiredCharge; // 必要な溜めレベル（None, Level1, Level2）

    [Header("Damage")]
    [SerializeField] private float _damageMultiplier = 1; // 攻撃力倍率（例: 1.5 = 150%のダメージ）

    [Header("Range")]
    [SerializeField] private float _attackRange = 1; // 攻撃の届く距離（例: 1.5 = 1.5m先まで攻撃が届く）
    [SerializeField] private float _attackRadius = 1; // 攻撃の当たり判定の半径（例: 0.5 = 攻撃の中心から0.5m以内がヒット範囲）

    [Header("Combo")]
    [SerializeField] private int _nextComboAttackId = -1; // 次のコンボ攻撃ID。-1の場合はコンボ終了。

    [Header("Knockback")]
    [SerializeField] private bool _enableKnockback = false; // ノックバックを有効にするかどうか
    [SerializeField] private float _knockbackPower = 5f; // ノックバックの強さ（
    [SerializeField] private float _knockbackUpward = 0f; // ノックバックの垂直成分

    [Header("Homing")]
    [SerializeField] private bool _enableHoming = false; // ホーミングを有効にするかどうか
    [SerializeField] private float _homingRadius = 5f; // ホーミングの探索半径
    [SerializeField] private float _homingAngle = 45f; // ホーミングの探索角度
    [SerializeField] private float _homingStrength = 10f; // ホーミングの強さ（大きいほどターゲットに向かって急激に曲がる）

    [Header("Movement")]
    [SerializeField] private AttackMoveType _moveType = AttackMoveType.None; // 攻撃中の移動タイプ
    [SerializeField] private float _moveDistance = 0f;
    [SerializeField] private float _moveSpeed = 0f;
    [SerializeField] private float _moveDuration = 0f;
    [SerializeField] private bool _stopOnHit = true;
    [SerializeField] private bool _isPhantom = false; // すり抜け攻撃かどうか 

    [Header("Hit Stop")]
    [SerializeField] private HitStopData _hitStopData;

    [Header("Animation")]
    [SerializeField] private string _animationStateName; // Animatorのステート名
    [SerializeField] private float _transitionDuration = -1f; // 遷移時間（秒）。-1の場合はデフォルト値(0.1f)を使用
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
