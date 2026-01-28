using UnityEngine;

public class MoveBehaviour : IEnemyBehaviour
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
        if (!_context.CanMove) return;
        if (_player == null) return;

        Vector3 dir = (_player.position - _self.position).normalized;
        dir.y = 0;

        _self.position += dir * _data.MoveSpeed * deltaTime;
    }

    private Transform _self;
    private Transform _player;
    private EnemyData _data;
    private EnemyContext _context;
}
