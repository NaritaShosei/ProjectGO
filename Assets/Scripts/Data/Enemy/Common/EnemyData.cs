using UnityEngine;

/// <summary>
/// Enemyの基本パラメータをまとめたデータ
/// </summary>
[CreateAssetMenu(fileName = "EnemyData", menuName = "GameData/Enemy/EnemyData")]
public class EnemyData : ScriptableObject
{
    // ステータス
    public float MaxHP => _maxHP;

    // 移動速度
    public float MoveSpeed => _moveSpeed;

    // 攻撃射程・判定・クールダウン・ダメージ
    public float AttackRange => _attackRange;
    public float AttackRadius => _attackRadius;
    public float AttackCooldown => _attackCooldown;
    public float AttackDamage => _attackDamage;

    // 攻撃パターン
    public EnemyAttackPattern AttackPattern => _attackPattern;

    // ノックバックレベル閾値
    public float KnockbackHitThreshold => _knockbackHitThreshold;
    public float KnockbackLargeThreshold => _knockbackLargeThreshold;

    // 停止後の硬直時間（秒）Hit / Small のみ使用
    public float KnockbackStunDuration => _knockbackStunDuration;

    // 水平方向の減速度（単位/秒²）。飛距離の目安 ≈ Power² / (2 × 減速度)
    public float KnockbackDeceleration => _knockbackDeceleration;

    // 前衛選出の優先度。値が高いほど前衛に選ばれやすい
    public float CombatPower => _combatPower;

    // Bark継続時間
    public float BarkDuration => _barkDuration;

    // スロット待ち時にBarkを選ぶ確率（0〜1）
    public float BarkChance => _barkChance;

    // 死亡時にヒールアイテムをドロップする確率（0〜1）
    public float HealDropChance => _healDropChance;


    [Header("Status")]
    [SerializeField] private float _maxHP = 100f;

    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 3f;

    [Header("Attack")]
    [SerializeField] private float _attackRange = 1.5f;
    [SerializeField] private float _attackRadius = 1.0f;
    [SerializeField] private float _attackCooldown = 1.2f;
    [SerializeField] private float _attackDamage = 10f;
    [SerializeField] private EnemyAttackPattern _attackPattern;

    [Header("Knockback")]
    // Power がこの値以下なら Hit（level 0）
    [SerializeField] private float _knockbackHitThreshold = 5f;
    // Power がこの値以上なら Large（level 2）
    [SerializeField] private float _knockbackLargeThreshold = 20f;
    [SerializeField] private float _knockbackStunDuration = 0.1f;
    [SerializeField] private float _knockbackDeceleration = 20f;

    [Header("Formation")]
    [SerializeField] private float _combatPower = 1f;

    [Header("Bark")]
    [SerializeField] private float _barkDuration = 2.0f;

    [Range(0f, 1f)]
    [SerializeField] private float _barkChance = 0.5f;

    [Header("Drop")]
    [Range(0f, 1f)]
    [SerializeField] private float _healDropChance = 0.3f;
}
