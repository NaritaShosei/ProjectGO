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
        // 攻撃の条件に満たしていなかったら早期リターン
        if (!_context.CanMove) { return; }
        if (_player == null) { return; }

        // TODO:雑に移動しているため場合によっては修正が必要
        Vector3 dir = (_player.position - _self.position);
        dir.y = 0;
        dir = dir.normalized;

        _self.position += dir * _data.MoveSpeed * deltaTime;
    }

    private Transform _self;
    private Transform _player;
    private EnemyData _data;
    private EnemyContext _context;
}
