using System;
using UnityEngine;

/// <summary>
/// プレイヤーを索敵距離外のときにランダム方向へ徘徊するBehaviour
/// ランダムな目標地点に向かって移動し、到達したら新しい目標を設定する
/// </summary>
public class RoamBehaviour : IEnemyBehaviour
{
    public int Priority { get => (int)EnemyBehaviourPriority.Roam; }

    /// <summary>
    /// DistanceProfile・各サービスはRoamBehaviour固有の依存のためコンストラクタで受け取る
    /// </summary>
    public RoamBehaviour(
        DistanceProfile profile,
        IEnemyAttackerSlot attackerSlot,
        ISeparationService separationService,
        IWallAvoidanceService wallAvoidanceService,
        ISpatialHashGrid spatialHashGrid,
        Action<Vector3?> onRoamDirection
    )
    {
        if (profile == null)
            throw new ArgumentNullException(nameof(profile));
        _profile = profile;
        _separationService = separationService;
        _attackerSlot = attackerSlot;
        _wallAvoidanceService = wallAvoidanceService;
        _spatialHashGrid = spatialHashGrid;
        _onRoamDirection = onRoamDirection;
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
        // Bark・Attack中はRoamに入らない
        if (_state == null) return false;
        return _state.CanMove();
    }

    // 目標地点に到達するまで継続（到達時はTick内でPickTargetが呼ばれるため
    // CanContinueで終了させず、Runner側の再選択に委ねる）
    public bool CanContinue()
    {
        // スロットが確保されたら即座に終了してMoveへ切り替わる
        if (_attackerSlot != null && _attackerSlot.IsAcquired(_enemyId)) return false;

        // 目標地点に到達していない間は継続
        Vector3 toTarget = _target - _self.position;
        toTarget.y = 0f;
        return toTarget.sqrMagnitude > _arrivalThreshold * _arrivalThreshold;
    }

    public void OnEnter()
    {
        _state.ChangeState(EnemyState.Move);

        // スポーン同期をずらすためランダムな初期待機時間を設定する
        if (!_delayFinished)
        {
            _initialDelay = UnityEngine.Random.Range(0f, 2f);
        }

        PickTarget();
    }

    public void OnExit()
    {
        _onRoamDirection?.Invoke(null);
        _state.ChangeState(EnemyState.Idle);
        _enemyAnimator?.SetSpeed(0f);
    }

    public void Tick(float deltaTime)
    {
        // 初期待機中はアニメーションなしで待機する
        if (!_delayFinished)
        {
            _initialDelay -= deltaTime;
            _enemyAnimator?.SetSpeed(0f);

            if (_initialDelay > 0f) return;

            _delayFinished = true;
        }

        if (!_state.CanMove()) return;

        // Speedを毎フレーム更新する
        _enemyAnimator?.SetSpeed(1f);

        Vector3 toTarget = _target - _self.position;
        toTarget.y = 0f;

        Vector3 oldPos = _self.position;
        Vector3 dir = toTarget.normalized;

        if (_separationService != null)
        {
            dir += _separationService.Calculate(
                _enemy,
                _self.position,
                _profile.SeparationRadius,
                _profile.SeparationStrength
            );
        }

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

        // 移動方向をTurnBehaviourに通知する
        _onRoamDirection?.Invoke(dir.normalized);

        Vector3 newPos = _self.position + dir.normalized * _data.MoveSpeed * deltaTime;
        _self.position = newPos;

        if (_spatialHashGrid != null)
        {
            _spatialHashGrid.UpdatePosition(_enemy, oldPos, newPos);
        }
    }

    private Transform _self;
    private IEnemy _enemy;
    private Transform _player;
    private EnemyData _data;
    private EnemyContext _context;
    private EnemyAnimator _enemyAnimator;
    private EnemyStateContext _state;

    private readonly DistanceProfile _profile;
    private readonly ISeparationService _separationService;
    private readonly IWallAvoidanceService _wallAvoidanceService;
    private readonly ISpatialHashGrid _spatialHashGrid;

    private int _enemyId;
    private readonly IEnemyAttackerSlot _attackerSlot;

    private Vector3 _target;

    // 目標地点への到達判定しきい値
    private const float _arrivalThreshold = 0.3f;

    // 初期待機タイマー（スポーン同期ずらし用）
    private float _initialDelay;
    private bool _delayFinished;
    private readonly Action<Vector3?> _onRoamDirection;

    /// <summary>
    /// RoamRadius内のランダムな目標地点を設定する
    /// </summary>
    private void PickTarget()
    {
        Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * _profile.RoamRadius;
        _target = _self.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
    }
}
