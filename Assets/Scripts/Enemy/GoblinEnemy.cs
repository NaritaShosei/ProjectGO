using UnityEngine;

public class GoblinEnemy : Enemy
{
    private Transform _player;
    private float _lastAttackTime;

    protected override void Awake()
    {
        base.Awake();
        // TODO:とりあえず雑に取得
        _player = FindAnyObjectByType<Player>().transform;
    }

    // TODO:移動や攻撃は別クラスで定義
    protected override void UpdateEnemy(float deltaTime)
    {
        if (_player == null) return;

        float distance = Vector3.Distance(transform.position, _player.position);

        if (distance > _data.AttackRange)
        {
            MoveToPlayer(deltaTime);
        }
        else
        {
            TryAttack();
        }
    }

    private void MoveToPlayer(float deltaTime)
    {
        Vector3 dir = (_player.position - transform.position).normalized;

        dir.y = 0;

        transform.position += dir * _data.MoveSpeed * deltaTime;
    }

    private void TryAttack()
    {
        if (Time.time - _lastAttackTime < _data.AttackCooldown)
            return;

        _lastAttackTime = Time.time;

        PerformAttack();
    }

    private void PerformAttack()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position + transform.forward * _data.AttackRange,
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
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_data == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward * _data.AttackRange, _data.AttackRadius);
    }
#endif
}
