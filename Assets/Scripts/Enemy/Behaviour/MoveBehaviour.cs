using UnityEngine;

// TODO: Contextへの依存を削除
public class MoveBehaviour : IEnemyBehaviour
{
    public void Init(
        Enemy owner,
        EnemyData data,
        Transform player,
        EnemyContext context,
        EnemyStateManager state
    )
    {
        _self = owner.transform;
        _player = player;
        _data = data;
        _context = context;
        _state = state;
    }

    public void Tick(float deltaTime)
    {
        // 攻撃の条件に満たしていなかったら早期リターン
        if (!_state.CanMove()) {  return; }
        if (!_context.CanMove) { return; }
        if (_player == null) { return; }

        _state.ChangeState(EnemyState.Moving);
        
        //TODO: ある程度プレイヤーに近づいたら停止する

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
    private EnemyStateManager _state;
}
