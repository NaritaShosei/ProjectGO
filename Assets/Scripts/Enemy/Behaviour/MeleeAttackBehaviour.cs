using UnityEngine;

public class MeleeAttackBehaviour : IEnemyBehaviour
{
    public void Init(Enemy owner, EnemyData data, Transform player)
    {
        _self = owner.transform;
        _player = player;
        _data = data;
    }

    public void Tick(float deltaTime)
    {
        if (_player == null) { return; }

        float distance = Vector3.Distance(
            _self.position,
            _player.position
        );

        if (distance > _data.AttackRange) { return; }
        if (Time.time - _lastAttackTime < _data.AttackCooldown) { return; }

        _lastAttackTime = Time.time;

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

    private float _lastAttackTime;
}
