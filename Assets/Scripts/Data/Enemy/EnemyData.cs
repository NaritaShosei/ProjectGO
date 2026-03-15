using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "GameData/EnemyData")]
public class EnemyData : ScriptableObject
{
    public float MaxHP => _maxHP;
    public float MoveSpeed => _moveSpeed;

    public float AttackRange => _attackRange;
    public float AttackRadius => _attackRadius;
    public float AttackCooldown => _attackCooldown;
    public float AttackDamage => _attackDamage;

    // Bark継続時間
    public float BarkDuration => _barkDuration;
    public float BarkChance => _barkChance;


    // 攻撃パターン
    public EnemyAttackPattern AttackPattern => _attackPattern;

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

    [Header("Bark")]
    [SerializeField] private float _barkDuration = 2.0f;

    // 追加：スロット待ち時にBarkを選ぶ確率（0〜1）
    [Range(0f, 1f)]
    [SerializeField] private float _barkChance = 0.5f;
}
