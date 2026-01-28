using Unity.AppUI.Core;
using UnityEngine;

public class MoveBehaviour : IEnemyBehaviour
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

        Vector3 dir = _player.position - _self.position;
        dir.y = 0;
        dir = dir.normalized;

        _self.position += dir * _data.MoveSpeed * deltaTime;
    }

    private Transform _self;
    private Transform _player;
    private EnemyData _data;
}
