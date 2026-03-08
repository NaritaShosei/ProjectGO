using UnityEngine;

/// <summary>
/// 攻撃距離内にいるがスロットが埋まっているときに威嚇するBehaviour
/// BarkDurationの時間が経過したら終了する
/// </summary>
public class BarkBehaviour : IEnemyBehaviour
{
    public int Priority { get => (int)EnemyBehaviourPriority.Bark; }

    /// <summary>
    /// DistanceProfile はBarkBehaviour固有の依存のためコンストラクタで受け取る
    /// </summary>
    public BarkBehaviour(DistanceProfile profile)
    {
        _profile = profile;
    }

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
    }

    public bool CanEnter()
    {
        if (_player == null) return false;

        // 攻撃距離内のときのみ発動
        float sqrDist = (_self.position - _player.position).sqrMagnitude;
        float sqrAttack = _profile.MinAttackDistance * _profile.MinAttackDistance;

        return sqrDist <= sqrAttack;
    }

    public bool CanContinue()
    {
        // タイマーが終わるまで継続
        return _timer < _data.BarkDuration;
    }

    public void OnEnter()
    {
        _timer = 0f;
        _state.ChangeState(EnemyState.Bark);
    }

    public void OnExit()
    {
        _state.ChangeState(EnemyState.Idle);
    }

    public void Tick(float deltaTime)
    {
        _timer += deltaTime;
    }

    private Transform _self;
    private Transform _player;
    private EnemyData _data;
    private EnemyContext _context;
    private EnemyStateContext _state;

    private readonly DistanceProfile _profile;

    private float _timer;
}
