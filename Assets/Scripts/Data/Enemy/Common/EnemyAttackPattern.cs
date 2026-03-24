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

    [Tooltip("攻撃を開始するトリガー距離の割合（1.0 = AttackRange, 1.5 = 遠めに攻撃開始）")]
    [Min(0f)]
    [SerializeField] private float _attackTriggerRatio = 1.0f;

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
    }
#endif
}
