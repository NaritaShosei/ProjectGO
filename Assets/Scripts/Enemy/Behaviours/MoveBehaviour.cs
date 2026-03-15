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
    public MoveBehaviour(
        DistanceProfile profile,
        IEnemyAttackerSlot attackerSlot,
        ISeparationService separationService,
        IWallAvoidanceService wallAvoidanceService,
        ISpatialHashGrid spatialHashGrid
    )
    {
        _profile = profile;
        _separationService = separationService;
        _wallAvoidanceService = wallAvoidanceService;
        _spatialHashGrid = spatialHashGrid;
        _attackerSlot = attackerSlot;
    }

    public void Init(
        Enemy owner,
        EnemyData data,
        Transform player,
        EnemyContext context,
        EnemyAnimator enemyAnimator,
        EnemyStateContext state
    )
    {
        _self = owner.transform;
        _enemy = owner;
        _enemyAnimator = enemyAnimator;
        _enemyId = owner.GetInstanceID();
        _player = player;
        _data = data;
        _context = context;
        _state = state;
    }

    public bool CanEnter()
    {
        Debug.Log($"[Move.CanEnter] player={_player != null}, slot={_attackerSlot != null}, acquired={(_attackerSlot != null ? _attackerSlot.IsAcquired(_enemyId).ToString() : "N/A")}, enemyId={_enemyId}");

        if (_player == null) return false;
        if (_attackerSlot == null) return false;

        // スロットを確保済みのときのみ発動
        if (!_attackerSlot.IsAcquired(_enemyId)) return false;

        float sqrDist = (_self.position - _player.position).sqrMagnitude;
        float sqrAttack = _profile.MinAttackDistance * _profile.MinAttackDistance;

        // 攻撃距離外のときに発動
        return sqrDist > sqrAttack;
    }

    public bool CanContinue()
    {
        if (_player == null) return false;
        if (_attackerSlot == null) return false;

        // スロットを解放されたら終了
        if (!_attackerSlot.IsAcquired(_enemyId)) return false;

        float sqrDist = (_self.position - _player.position).sqrMagnitude;
        float sqrAttack = _profile.MinAttackDistance * _profile.MinAttackDistance;

        // 攻撃距離内に入ったら終了
        return sqrDist > sqrAttack;
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

        // EnemyContextの距離を更新する
        _context.DistanceToPlayer = Vector3.Distance(
            _self.position,
            _player.position
        );
    }

    private Transform _self;
    private IEnemy _enemy;
    private Transform _player;
    private EnemyData _data;
    private EnemyContext _context;
    private EnemyStateContext _state;
    private int _enemyId;
    private EnemyAnimator _enemyAnimator;

    private readonly DistanceProfile _profile;
    private readonly ISeparationService _separationService;
    private readonly IWallAvoidanceService _wallAvoidanceService;
    private readonly ISpatialHashGrid _spatialHashGrid;
    private readonly IEnemyAttackerSlot _attackerSlot;
}
