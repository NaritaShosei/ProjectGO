using UnityEngine;

/// <summary>
/// 攻撃後にプレイヤーから距離を取るためのBehaviour
/// </summary>
public class RetreatBehaviour : IEnemyBehaviour
{

    public int Priority { get => (int)EnemyBehaviourPriority.Retreat; }

    /// <summary>
    /// DistanceProfile・各サービスはRetreat固有の依存のためコンストラクタで受け取る
    /// </summary>
    public RetreatBehaviour(DistanceProfile profile, EnemyServices services)
    {
        _profile = profile;
        _separationService = services.SeparationService;
        _wallAvoidanceService = services.WallAvoidanceService;
        _spatialHashGrid = services.SpatialHashGrid;
    }

    public void Init(BehaviourInitContext ctx)
    {
        _self = ctx.Owner.Self;
        _enemy = ctx.Owner;
        _player = ctx.Player;
        _context = ctx.RuntimeContext;
        _state = ctx.StateContext;
        _enemyAnimator = ctx.EnemyAnimator;
    }

    public bool CanEnter()
    {
        if (_player == null) return false;

        var request = _context.PendingRetreat;
        if (!request.Enabled) return false;

        return CalcXZSqrDist() < request.RetreatDistance * request.RetreatDistance;
    }

    public bool CanContinue()
    {
        if (_player == null) return false;

        var request = _context.PendingRetreat;
        if (!request.Enabled) return false;

        return CalcXZSqrDist() < request.RetreatDistance * request.RetreatDistance;
    }

    public void OnEnter()
    {
        _state.ChangeState(EnemyState.Move);
        _enemyAnimator?.SetSpeed(1f);
    }

    public void OnExit()
    {
        // 消費済みにする（同じリクエストで再入場しないように）
        var request = _context.PendingRetreat;
        request.Enabled = false;
        _context.PendingRetreat = request;

        _state.ChangeState(EnemyState.Idle);
        _enemyAnimator?.SetSpeed(0f);
    }

    public void Tick(float deltaTime)
    {
        if (_player == null) return;
        if (!_state.CanMove()) return;

        var request = _context.PendingRetreat;

        Vector3 oldPos = _self.position;

        // プレイヤーから離れる方向を基本ベクトルとする
        Vector3 dir = _self.position - _player.position;
        dir.y = 0f;
        dir = dir.normalized;

        // 分離力を加算する（後退中に敵同士が重ならないようにする）
        if (_separationService != null)
        {
            dir += _separationService.Calculate(
                _enemy,
                _self.position,
                _profile.SeparationRadius,
                _profile.SeparationStrength
            );
        }

        // 壁を背にして後退し続けないよう壁回避力を加算する
        if (_wallAvoidanceService != null)
        {
            dir += _wallAvoidanceService.CalculateAvoidance(
                _self.position,
                dir.normalized,
                _profile.WallDetectDistance,
                _profile.WallAvoidanceStrength
            );
        }

        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;

        Vector3 displacement = dir.normalized * request.RetreatSpeed * deltaTime;
        if (_enemy is Enemy movableEnemy)
            movableEnemy.Move(displacement);
        else
            _self.position += displacement;

        Vector3 newPos = _self.position;
        if (_spatialHashGrid != null)
        {
            _spatialHashGrid.UpdatePosition(_enemy, oldPos, newPos);
        }

        _context.DistanceToPlayer = Vector3.Distance(_self.position, _player.position);
    }


    private Transform _self;
    private IEnemy _enemy;
    private Transform _player;
    private EnemyRuntimeContext _context;
    private EnemyStateContext _state;
    private IEnemyAnimator _enemyAnimator;

    private readonly DistanceProfile _profile;
    private readonly ISeparationService _separationService;
    private readonly IWallAvoidanceService _wallAvoidanceService;
    private readonly ISpatialHashGrid _spatialHashGrid;

    /// <summary>
    /// XZ平面のみの距離の二乗を返す
    /// </summary>
    private float CalcXZSqrDist()
    {
        float dx = _self.position.x - _player.position.x;
        float dz = _self.position.z - _player.position.z;
        return dx * dx + dz * dz;
    }
}

//攻撃のホーミングは常にではなく、前進はじめのみホーミングさせ、
//前進時（切りかかり～攻撃）はホーミングしないようにしてほしいです

//プレイヤーとの距離と、攻撃をした後の硬直、プレイヤーに近づいた場合どれくらいの移動速度でまた距離を取り直すのか、をインスペクターから調整できるようにしたいです🙏 
//すぐに距離を取り直してしまうと、闘神モードのようなその場でチャージ攻撃をするモードと相性がとても悪くなってしまうと思いました
