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

    [Header("Status")]
    [SerializeField] private float _maxHP = 100f;

    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 3f;

    // TODO:攻撃データはわけて作る

    [Header("Attack")]
    [SerializeField] private float _attackRange = 1.5f;
    [SerializeField] private float _attackRadius = 1.0f;
    [SerializeField] private float _attackCooldown = 1.2f;
    [SerializeField] private float _attackDamage = 10f;
}
