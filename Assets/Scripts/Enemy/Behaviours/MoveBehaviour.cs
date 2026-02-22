using UnityEngine;


/// <summary>
/// 中身は後で実装
/// </summary>
public class MoveBehaviour : IEnemyBehaviour
{

    public int Priority { get => (int)EnemyBehaviourPriority.Move; }

    public bool CanEnter() { return true; }
    public bool CanContinue() { return true; }

    public void OnEnter() { }
    public void OnExit() { }

    public void Init(
        Enemy owner,
        EnemyData data,
        Transform player,
        EnemyContext context,
        EnemyStateContext state
    )
    {
        _self = owner.transform;
        _player = player;
        _data = data;
        _context = context;
        _state = state;

        // 本来は敵ごとにEnemyDataに定義するもの
        _maxApproachLimit = 2f;
    }

    public void Tick(float deltaTime)
    {
        // 攻撃の条件に満たしていなかったら早期リターン
        if (!_state.CanMove()) { return; }
        if (_player == null) { return; }


        // プレイヤーに十分に近ければ動かない
        if (IsWithinDistance(_self.position, _player.position, _maxApproachLimit)) { return; }

        _state.ChangeState(EnemyState.Move);

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
    private EnemyStateContext _state;

    private float _maxApproachLimit;

    bool IsWithinDistance(Vector3 self, Vector3 player, float threshold)
    {
        float sqrDist = (self - player).sqrMagnitude;
        float sqrThreshold = threshold * threshold;
        return sqrDist <= sqrThreshold;
    }
}
