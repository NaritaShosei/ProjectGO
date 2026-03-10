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

    [SerializeField] private string _patternName;

    [Header("Slot")]
    [Min(1)]
    [SerializeField] private int _slotCost = 1;

    [Header("Timing")]
    [Min(0f)]
    [SerializeField] private float _windUp;

    [Min(0f)]
    [SerializeField] private float _duration;

    [Min(0f)]
    [SerializeField] private float _cooldown;

    [Header("Hit")]
    [Min(1)]
    [SerializeField] private int _maxHitCount = 1;

    [Min(0f)]
    [SerializeField] private float _hitInterval = 0.2f;

    [Header("Knockback")]
    [Min(0f)]
    [SerializeField] private float _knockbackPower;

    [Header("Damage")]
    [Min(0)]
    [SerializeField] private int _baseDamage;

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
