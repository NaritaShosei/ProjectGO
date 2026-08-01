using System;
using UnityEngine;

/// <summary>
/// 他のBehaviourが選択できないときのフォールバックとしてランダム方向へ徘徊するBehaviour
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
        EnemyServices services,
        Action<Vector3?> onRoamDirection
    )
    {
        if (profile == null)
            throw new ArgumentNullException(nameof(profile));
        _profile = profile;
        _attackerSlot = services.AttackerSlot;
        _separationService = services.SeparationService;
        _wallAvoidanceService = services.WallAvoidanceService;
        _spatialHashGrid = services.SpatialHashGrid;
        _onRoamDirection = onRoamDirection;
    }

    public void Init(BehaviourInitContext ctx)
    {
        _self = ctx.Owner.Self;
        _enemy = ctx.Owner;
        _enemyId = ctx.Owner.Id;
        _enemyAnimator = ctx.EnemyAnimator;
        _player = ctx.Player;
        _data = ctx.Data;
        _state = ctx.StateContext;
    }

    public bool CanEnter()
    {
        // フォールバックBehaviourのため常時enterable
        // Attack・Bark中はCanMove()でブロックされる
        if (_state == null) return false;
        return _state.CanMove();
    }

    public bool CanContinue()
    {
        // 目標地点に到達していない間は継続
        Vector3 toTarget = _target - _self.position;
        toTarget.y = 0f;
        return toTarget.sqrMagnitude > _arrivalThreshold * _arrivalThreshold;
    }

    public void OnEnter()
    {
        _state.ChangeState(EnemyState.Move);

        // スポーン同期ずらし用の初期待機時間をランダムで設定する
        _initialDelay = UnityEngine.Random.Range(0f, _initialDelayMax);
        _delayFinished = _initialDelay <= 0f;
        
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
        float walkSpeed = 0;

        // 初期待機中はアニメーションなしで待機する
        if (!_delayFinished)
        {
            _initialDelay -= deltaTime;
            _enemyAnimator?.SetSpeed(0f);

            if (_initialDelay > 0f) return;

            _delayFinished = true;
        }

        if (!_state.CanMove()) return;

        // 後ろに下がる挙動のときは歩行速度をマイナスにする
        if (_isBackStep) walkSpeed = -0.5f;
        else walkSpeed = 0.5f;

        // Speedを毎フレーム更新する
        _enemyAnimator?.SetSpeed(walkSpeed);

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
        if (_isBackStep)
        {
            // 後ろに下がる挙動のときはTurnBehaviourにPlayerの方向を向かせる
            _onRoamDirection?.Invoke(null);
        }
        else
        {
            _onRoamDirection?.Invoke(dir.normalized);
        }

        Vector3 displacement = dir.normalized * _data.RoamSpeed * deltaTime;
        if (_enemy is Enemy movableEnemy)
            movableEnemy.Move(displacement);
        else
            _self.position += displacement;
        Vector3 newPos = _self.position;

        if (_spatialHashGrid != null)
        {
            _spatialHashGrid.UpdatePosition(_enemy, oldPos, newPos);
        }
    }

    private Transform _self;
    private IEnemy _enemy;
    private Transform _player;
    private EnemyData _data;
    private IEnemyAnimator _enemyAnimator;
    private EnemyStateContext _state;

    private readonly DistanceProfile _profile;
    private readonly IEnemyAttackerSlot _attackerSlot;
    private readonly ISeparationService _separationService;
    private readonly IWallAvoidanceService _wallAvoidanceService;
    private readonly ISpatialHashGrid _spatialHashGrid;

    private int _enemyId;

    private Vector3 _target;

    // 目標地点への到達判定しきい値
    private const float _arrivalThreshold = 0.3f;
    // 初期待機のランダム上限（スポーン同期ずらし用）
    private const float _initialDelayMax = 0.5f;

    // 初期待機タイマー（スポーン同期ずらし用）
    private float _initialDelay;
    private bool _delayFinished;
    private readonly Action<Vector3?> _onRoamDirection;

    // 後ろに下がる挙動のフラグ
    private bool _isBackStep;

    /// <summary>
    /// プレイヤーとの距離・攻撃参加状態に応じて移動目標を設定する
    /// 遠すぎる場合はプレイヤーへ、非攻撃者が近すぎる場合はプレイヤーから離れる方向、
    /// それ以外はプレイヤー方向から ±90° 以内のランダム方向へ RoamRadius 分移動する
    /// </summary>
    private void PickTarget()
    {
        Vector3 toPlayer = _player.position - _self.position;
        toPlayer.y = 0f;
        float distToPlayer = toPlayer.magnitude;

        bool isAttacker = _attackerSlot != null && _attackerSlot.IsAcquired(_enemyId);

        Vector3 baseDir;

        if (distToPlayer > _profile.MaxRoamDistance)
        {
            // プレイヤーから離れすぎているため、プレイヤー方向へ向かう
            baseDir = distToPlayer > 0f ? toPlayer.normalized : Vector3.forward;
        }
        else if (!isAttacker && distToPlayer < _profile.MinNonAttackerDistance)
        {
            // 非攻撃者が近づきすぎているため、プレイヤーと逆方向から ±90° 以内のランダム方向へ向かう
            Vector3 awayFromPlayer = distToPlayer > 0f ? -toPlayer.normalized : Vector3.back;
            float angle = UnityEngine.Random.Range(-90f, 90f);
            baseDir = Quaternion.Euler(0f, angle, 0f) * awayFromPlayer;

            _isBackStep = true; // 後ろに下がる挙動を有効化
        }
        else if (isAttacker && distToPlayer < _profile.MinAttackerRoamDistance)
        {
            // 攻撃者が最小距離より近い場合はプレイヤーから離れる方向へ徘徊する
            Vector3 awayFromPlayer = distToPlayer > 0f ? -toPlayer.normalized : Vector3.back;
            float angle = UnityEngine.Random.Range(-90f, 90f);
            baseDir = Quaternion.Euler(0f, angle, 0f) * awayFromPlayer;

            _isBackStep = true; // 後ろに下がる挙動を有効化
        }
        else if (isAttacker)
        {
            // 攻撃者はプレイヤーを中心とした円周上を移動する（横方向のみ）
            Vector3 dirToPlayer = distToPlayer > 0f ? toPlayer.normalized : Vector3.forward;
            Vector3 lateral = Vector3.Cross(Vector3.up, dirToPlayer).normalized;
            float sign = UnityEngine.Random.value < 0.5f ? 1f : -1f;
            baseDir = lateral * sign;

            _isBackStep = true; // 後ろに下がる挙動を有効化
        }
        else
        {
            // 非攻撃者はプレイヤー方向を基準に ±90° 以内のランダムな方向へ徘徊する
            Vector3 dirToPlayer = distToPlayer > 0f ? toPlayer.normalized : Vector3.forward;
            float angle = UnityEngine.Random.Range(-90f, 90f);
            baseDir = Quaternion.Euler(0f, angle, 0f) * dirToPlayer;

            _isBackStep = true; // 後ろに下がる挙動を有効化
        }

        _target = _self.position + baseDir * _profile.RoamRadius;
    }
}
