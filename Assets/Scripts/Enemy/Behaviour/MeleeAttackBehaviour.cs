using UnityEngine;

public class MeleeAttackBehaviour : IEnemyBehaviour
{
    public void Init(
        Enemy owner,
        EnemyData data,
        Transform player,
        EnemyContext context
    )
    {
        _self = owner.transform;
        _player = player;
        _data = data;
        _context = context;
    }

    public void Tick(float deltaTime)
    {
        if (_player == null) return;

        _context.DistanceToPlayer = Vector3.Distance(
            _self.position,
            _player.position
        );

        if (_context.DistanceToPlayer > _data.AttackRange) return;
        if (Time.time - _lastAttackTime < _data.AttackCooldown) return;

        _context.IsAttacking = true;
        _lastAttackTime = Time.time;

        PerformAttack();

        _context.IsAttacking = false;
    }

    private void PerformAttack()
    {
        Collider[] hits = Physics.OverlapSphere(
            _self.position + _self.forward * _data.AttackRange,
            _data.AttackRadius
        );

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IPlayer>(out var player))
            {
                player.TakeDamage(_data.AttackDamage);
            }
        }
    }

    private Transform _self;
    private Transform _player;
    private EnemyData _data;
    private EnemyContext _context;

    private float _lastAttackTime;
}
