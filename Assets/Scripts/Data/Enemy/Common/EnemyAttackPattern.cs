using UnityEngine;

/// <summary>
/// 敵の攻撃パターンを定義するScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "EnemyAttackPattern", menuName = "GameData/Enemy/EnemyAttackPattern")]
public sealed class EnemyAttackPattern : ScriptableObject
{
    public string PatternName => _patternName;

    // 攻撃時に占有するスロット数（1以上）
    public int SlotCost => _slotCost;

    // 攻撃前の溜め時間
    public float WindUp => _windUp;

    // 攻撃の持続時間
    public float Duration => _duration;

    // 攻撃後のクールダウン
    public float Cooldown => _cooldown;

    // 攻撃中の最大ヒット数
    public int MaxHitCount => _maxHitCount;

    // 複数ヒット時のヒット間隔
    public float HitInterval => _hitInterval;

    // ノックバックの強さ
    public float KnockbackPower => _knockbackPower;

    // 基礎ダメージ量
    public int BaseDamage => _baseDamage;

    // 攻撃射程（当たり判定・Move停止距離の基準）
    public float AttackRange => _attackRange;

    // 攻撃の当たり判定半径
    public float AttackRadius => _attackRadius;

    // 攻撃を開始するトリガー距離（AttackRange * AttackTriggerRatio）
    public float AttackTriggerRatio => _attackTriggerRatio;

    public bool EnableMovement => _enableMovement;
    public AnimationCurve MoveCurve => _moveCurve;
    public float MoveDistance => _moveDistance;
    public float MoveDuration => _moveDuration;
    public float KeepDistance => _keepDistance;

    public bool EnableHoming => _enableHoming;
    public float HomingRadius => _homingRadius;
    public float HomingAngle => _homingAngle;
    public float HomingStrength => _homingStrength;
    public float HomingDuration => _homingDuration;

    [SerializeField] private string _patternName;

    [Header("Slot")]
    [Tooltip("将来使用予定。現在スロット制御では使用しない（アタッカースロットは人数ベースで管理）")]
    [Min(1)]
    [SerializeField] private int _slotCost = 1;

    [Header("Timing")]
    [Tooltip("溜め時間。EnemyAttackSMBの_attackHitTimeと合わせて設定する（コードでは直接参照しない）")]
    [Min(0f)]
    [SerializeField] private float _windUp;

    [Tooltip("攻撃持続時間（秒）。SMBのOnStateExitが正常に発火しない場合のフォールバックとして使用する")]
    [Min(0f)]
    [SerializeField] private float _duration = 3.0f;

    [Min(0f)]
    [SerializeField] private float _cooldown = 1.5f;

    [Header("Hit")]
    [Min(1)]
    [SerializeField] private int _maxHitCount = 1;

    [Min(0f)]
    [SerializeField] private float _hitInterval = 0.2f;

    [Header("Knockback")]
    [Tooltip("プレイヤーへのノックバック強度。現在PerformHit()で未使用（将来の実装用）")]
    [Min(0f)]
    [SerializeField] private float _knockbackPower;

    [Header("Damage")]
    [Min(0)]
    [SerializeField] private int _baseDamage = 10;

    [Header("Range")]
    [Min(0f)]
    [SerializeField] private float _attackRange = 3.0f;

    [Min(0f)]
    [SerializeField] private float _attackRadius = 1.0f;

    [Tooltip("攻撃を開始するトリガー距離の割合（1.0 = AttackRange, 1.5 = 遠めに攻撃開始）\n0は AttackRange × 0 = 0 になり攻撃に入れなくなるため 0.01 以上を強制する")]
    [Min(0.01f)]
    [SerializeField] private float _attackTriggerRatio = 1.0f;


    [Header("Movement")]
    [Tooltip("攻撃中に前進（突進）させるかどうか")]
    [SerializeField] private bool _enableMovement = false;
    [Tooltip("前進の進み方を0〜1で定義するカーブ。X=経過時間の割合(0〜1)、Y=移動距離の割合(0〜1)")]
    [SerializeField] private AnimationCurve _moveCurve = AnimationCurve.Linear(0, 0, 1, 1);
    [Tooltip("前進の合計移動距離（MoveCurveのYが1のときにこの距離だけ進む）")]
    [Min(0f)]
    [SerializeField] private float _moveDistance = 0f;
    [Tooltip("前進を開始してから完了するまでの秒数")]
    [Min(0.01f)]
    [SerializeField] private float _moveDuration = 0.3f;
    [Tooltip("前進中、プレイヤーとこれ以上は詰めない距離")]
    [Min(0f)]
    [SerializeField] private float _keepDistance = 0.5f;

    [Header("Homing")]
    [Tooltip("前進はじめのみプレイヤー方向へ向き補正を行うかどうか")]
    [SerializeField] private bool _enableHoming = false;
    [Tooltip("この距離より遠いプレイヤーには補正しない")]
    [Min(0f)]
    [SerializeField] private float _homingRadius = 5f;
    [Tooltip("この角度を超える補正は行わない（急旋回防止）")]
    [Range(0f, 180f)]
    [SerializeField] private float _homingAngle = 60f;
    [Tooltip("向き補正の回転速度（度/秒）")]
    [Min(0f)]
    [SerializeField] private float _homingStrength = 180f;
    [Tooltip("前進開始からこの秒数の間だけ補正を行う（絶対時間）")]
    [Min(0f)]
    [SerializeField] private float _homingDuration = 0.15f;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // MaxHitCount > 1 のとき HitInterval が 0 だと
        // ヒット処理が瞬時に連続して意図しない挙動になる
        if (_maxHitCount > 1 && _hitInterval <= 0f)
        {
            _hitInterval = 0.1f;
            Debug.LogWarning(
                $"[EnemyAttackPattern] MaxHitCount > 1 のとき HitInterval は 0 より大きい必要があります。0.1 に補正しました。",
                this
            );
        }

        // HitInterval が Duration を超えると1回もヒットしない
        if (_maxHitCount > 1 && _hitInterval > _duration)
        {
            _hitInterval = _duration;
            Debug.LogWarning(
                $"[EnemyAttackPattern] HitInterval が Duration ({_duration}) を超えています。Duration に補正しました。",
                this
            );
        }

        // AttackTriggerRatio が 0 だと AttackRange × 0 = 0 になり Attack に入れなくなる
        if (_attackTriggerRatio < 0.01f)
        {
            _attackTriggerRatio = 0.01f;
            Debug.LogWarning(
                "[EnemyAttackPattern] AttackTriggerRatio は 0.01 以上が必要です。0.01 に補正しました。",
                this
            );
        }

        // MoveDuration が 0 だと Evaluate(moveT) がゼロ除算になる
        if (_enableMovement && _moveDuration <= 0f)
        {
            _moveDuration = 0.01f;
            Debug.LogWarning(
                "[EnemyAttackPattern] MoveDuration は 0 より大きい必要があります。0.01 に補正しました。",
                this
            );
        }

        // HomingDurationがMoveDurationを超えると「はじめのみ」の意味がなくなる
        if (_enableMovement && _enableHoming && _homingDuration > _moveDuration)
        {
            _homingDuration = _moveDuration;
            Debug.LogWarning(
                "[EnemyAttackPattern] HomingDuration が MoveDuration を超えています。MoveDuration に補正しました。",
                this
            );
        }
    }
#endif
}
