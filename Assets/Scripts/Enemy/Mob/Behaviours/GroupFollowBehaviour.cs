using UnityEngine;

/// <summary>
/// 攻撃役ではないグループメンバーを、攻撃役の真後ろへ追従させる。
/// </summary>
public sealed class GroupFollowBehaviour
    : IEnemyBehaviour, IEnemyDataRefreshable
{
    public int Priority => (int)EnemyBehaviourPriority.GroupFollow;

    public GroupFollowBehaviour(
        IEnemyGroupMember groupMember,
        EnemyServices services,
        float followDistance,
        float formationHalfWidth,
        float rearDistance,
        float stopDistance)
    {
        _groupMember = groupMember;
        _followDistance = Mathf.Max(0.1f, followDistance);
        _formationHalfWidth = Mathf.Max(0.1f, formationHalfWidth);
        _rearDistance = Mathf.Max(_followDistance, rearDistance);
        _stopDistance = Mathf.Max(0.01f, stopDistance);
        _wallAvoidanceService = services.WallAvoidanceService;
        _spatialHashGrid = services.SpatialHashGrid;
    }

    public void Init(BehaviourInitContext ctx)
    {
        _enemy = ctx.Owner;
        _self = ctx.Owner.Self;
        _data = ctx.Data;
        _state = ctx.StateContext;
        _enemyAnimator = ctx.EnemyAnimator;
    }

    public bool CanEnter() => CanFollow();
    public bool CanContinue() => CanFollow();

    public void OnEnter()
    {
        _state.ChangeState(EnemyState.Move);
    }

    /// <summary>
    /// グループの攻撃役の真後ろへ追従する。
    /// </summary>
    /// <param name="deltaTime"></param>
    public void Tick(float deltaTime)
    {
        if (!CanFollow()) return;

        EnemyGroup group = _groupMember.Group;

        if (!group.TryGetFollowerIndex(
                _enemy,
                out int followerIndex))
            return;

        if (!TryGetAttackerBasis(
                group,
                out Vector3 attackerCenter,
                out Vector3 attackerForward,
                out Vector3 attackerRight))
            return;

        Vector3 targetPosition = GetPentagonPosition(
            followerIndex,
            attackerCenter,
            attackerForward,
            attackerRight);

        Vector3 direction = targetPosition - _self.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= _stopDistance * _stopDistance)
        {
            _enemyAnimator?.SetSpeed(0f);
            return;
        }

        Vector3 oldPosition = _self.position;
        direction.Normalize();

        if (_wallAvoidanceService != null)
        {
            direction += _wallAvoidanceService.CalculateAvoidance(
                _self.position,
                direction,
                WallDetectDistance,
                WallAvoidanceStrength);
        }

        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) return;

        Vector3 displacement =
            direction.normalized * _data.ApproachSpeed * deltaTime;

        if (_enemy is Enemy movableEnemy)
            movableEnemy.Move(displacement);
        else
            _self.position += displacement;

        _spatialHashGrid?.UpdatePosition(
            _enemy,
            oldPosition,
            _self.position);

        _enemyAnimator?.SetSpeed(1f);
    }

    public void OnExit()
    {
        _enemyAnimator?.SetSpeed(0f);
        _state.ChangeState(EnemyState.Idle);
    }

    public void RefreshData(EnemyData data)
    {
        _data = data;
    }

    /// <summary>
    /// グループの攻撃役の位置と向きを計算する。
    /// </summary>
    /// <returns></returns>
    private bool TryGetAttackerBasis(
        EnemyGroup group,
        out Vector3 center,
        out Vector3 forward,
        out Vector3 right)
    {
        center = Vector3.zero;
        forward = Vector3.zero;
        right = Vector3.zero;
        int count = 0;
        Vector3 fallbackForward = Vector3.zero;

        foreach (IEnemy attacker in group.Attackers)
        {
            if (attacker == null ||
                attacker.IsDead ||
                attacker.Self == null)
            {
                continue;
            }

            if (fallbackForward.sqrMagnitude < 0.001f)
                fallbackForward = attacker.Self.forward;

            center += attacker.Self.position;
            forward += attacker.Self.forward;
            count++;
        }

        if (count == 0) return false;

        center /= count;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
            forward = fallbackForward;

        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) return false;

        forward.Normalize();
        right = Vector3.Cross(Vector3.up, forward).normalized;
        return true;
    }

    /// <summary>
    /// 五角形の計算
    /// </summary
    /// <returns></returns>
    private Vector3 GetPentagonPosition(
        int followerIndex,
        Vector3 center,
        Vector3 forward,
        Vector3 right)
    {
        switch (followerIndex)
        {
            // 左右の中段
            case 0:
                return center - forward * _followDistance
                    - right * _formationHalfWidth;

            case 1:
                return center - forward * _followDistance
                    + right * _formationHalfWidth;

            // 五角形の後端
            case 2:
                return center - forward * _rearDistance;

            // 6体以上になった場合は後端からさらに後ろへ並べる
            default:
                int extraRow =
                    (followerIndex - 3) / 2 + 1;
                float sideSign = followerIndex % 2 == 0 ? -1f : 1f;
                return center
                    - forward * (_rearDistance + _followDistance * extraRow)
                    + right * _formationHalfWidth * sideSign;
        }
    }

    private bool CanFollow()
    {
        if (_enemy == null || _enemy.IsDead || _self == null ||
            _data == null || _state == null || !_state.CanMove())
            return false;

        EnemyGroup group = _groupMember.Group;

        return group != null &&
               group.Phase == EnemyGroupPhase.Released &&
               !group.IsAttacker(_enemy.Id) &&
               group.Attackers.Count > 0;
    }

    private readonly IEnemyGroupMember _groupMember;
    private readonly IWallAvoidanceService _wallAvoidanceService;
    private readonly ISpatialHashGrid _spatialHashGrid;
    private readonly float _followDistance;
    private readonly float _formationHalfWidth;
    private readonly float _rearDistance;
    private readonly float _stopDistance;

    private IEnemy _enemy;
    private Transform _self;
    private EnemyData _data;
    private EnemyStateContext _state;
    private IEnemyAnimator _enemyAnimator;

    private const float WallDetectDistance = 2f;
    private const float WallAvoidanceStrength = 1.5f;
}
