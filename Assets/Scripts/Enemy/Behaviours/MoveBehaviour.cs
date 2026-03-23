using UnityEngine;

/// <summary>
/// プレイヤーに向かって移動するBehaviour
/// SeparationServiceとWallAvoidanceServiceで移動方向を補正する
/// </summary>
public class MoveBehaviour : IEnemyBehaviour
{
    public int Priority { get => (int)EnemyBehaviourPriority.Move; }

    /// <summary>
    /// DistanceProfile・各サービスはMove固有の依存のためコンストラクタで受け取る
    /// </summary>
    public MoveBehaviour(DistanceProfile profile, EnemyServices services)
    {
        _profile = profile;
        _separationService = services.SeparationService;
        _wallAvoidanceService = services.WallAvoidanceService;
        _spatialHashGrid = services.SpatialHashGrid;
        _attackerSlot = services.AttackerSlot;
    }

    public void Init(BehaviourInitContext ctx)
    {
        _self = ctx.Owner.GetTargetCenter();
        _enemy = ctx.Owner;
        _enemyAnimator = ctx.EnemyAnimator;
        _enemyId = ctx.Owner.Id;
        _player = ctx.Player;
        _data = ctx.Data;
        _context = ctx.RuntimeContext;
        _state = ctx.StateContext;
    }

    public bool CanEnter()
    {
        if (_player == null) return false;
        if (_attackerSlot == null) return false;

        // スロットを確保済みのときのみ発動
        if (!_attackerSlot.IsAcquired(_enemyId)) return false;

        // パターン未選択なら発動しない（MobEnemy.UpdateEnemy()が次フレームで選択する）
        if (_context.SelectedPattern == null) return false;

        float sqrDist = (_self.position - _player.position).sqrMagnitude;
        // AttackRangeの_approachRatio倍まで近づいていないときに発動
        float stop = _context.SelectedPattern.AttackRange * _profile.MoveApproachRatio;

        // 攻撃可能距離まで近づいていないときに発動
        return sqrDist > stop * stop;
    }

    public bool CanContinue()
    {
        if (_player == null) return false;
        if (_attackerSlot == null) return false;

        // スロットを解放されたら終了
        if (!_attackerSlot.IsAcquired(_enemyId)) return false;

        // パターンがクリアされたら終了
        if (_context.SelectedPattern == null) return false;

        float sqrDist = (_self.position - _player.position).sqrMagnitude;
        float stop = _context.SelectedPattern.AttackRange * _profile.MoveApproachRatio;

        // 攻撃可能距離内に入ったら終了
        return sqrDist > stop * stop;
    }

    public void OnEnter()
    {
        _state.ChangeState(EnemyState.Move);
        _enemyAnimator?.SetSpeed(1f);
    }

    public void OnExit()
    {
        _state.ChangeState(EnemyState.Idle);
        _enemyAnimator?.SetSpeed(0f);
    }

    public void Tick(float deltaTime)
    {
        if (_player == null) return;
        if (!_state.CanMove()) return;

        Vector3 oldPos = _self.position;

        // プレイヤーへの方向を基本ベクトルとする
        Vector3 dir = _player.position - _self.position;
        dir.y = 0f;
        dir = dir.normalized;

        // 分離力を加算する
        if (_separationService != null)
        {
            dir += _separationService.Calculate(
                _enemy,
                _self.position,
                _profile.SeparationRadius,
                _profile.SeparationStrength
            );
        }

        // 壁回避力を加算する
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

        // 方向ベクトルが極端に小さい場合はスキップ
        if (dir.sqrMagnitude < 0.001f) return;

        Vector3 newPos = _self.position + dir.normalized * _data.MoveSpeed * deltaTime;
        _self.position = newPos;

        // SpatialHashGridの位置を更新する
        if (_spatialHashGrid != null)
        {
            _spatialHashGrid.UpdatePosition(_enemy, oldPos, newPos);
        }

        // EnemyRuntimeContextの距離を更新する
        _context.DistanceToPlayer = Vector3.Distance(
            _self.position,
            _player.position
        );
    }

    private Transform _self;
    private IEnemy _enemy;
    private Transform _player;
    private EnemyData _data;
    private EnemyRuntimeContext _context;
    private EnemyStateContext _state;
    private int _enemyId;
    private IEnemyAnimator _enemyAnimator;

    private readonly DistanceProfile _profile;
    private readonly ISeparationService _separationService;
    private readonly IWallAvoidanceService _wallAvoidanceService;
    private readonly ISpatialHashGrid _spatialHashGrid;
    private readonly IEnemyAttackerSlot _attackerSlot;
}
