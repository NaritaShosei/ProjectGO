using System;
using UnityEngine;

/// <summary>
/// プレイヤーの方向に向き続けるBehaviour
/// EnemyBehaviourRunnerの並列スロットで毎フレーム実行される
/// </summary>
public class TurnBehaviour : IEnemyBehaviour
{
    // Turnは優先度の概念外（並列実行）のためNoneとする
    public int Priority { get => (int)EnemyBehaviourPriority.None; }

    public bool IsFinished { get; private set; }

    /// <summary>
    /// TurnProfile はTurnBehaviour固有の依存のためコンストラクタで受け取る
    /// </summary>
    public TurnBehaviour(TurnProfile profile)
    {
        if (profile == null)
            throw new ArgumentNullException(nameof(profile));

        _profile = profile;
    }

    /// <summary>
    /// Roam中など、プレイヤー以外の方向を向かせる場合に使用する
    /// nullを渡すとプレイヤー方向に戻る
    /// </summary>
    public void SetOverrideDirection(Vector3? direction)
    {
        _overrideDirection = direction;
    }

    public void Init(BehaviourInitContext ctx)
    {
        _self = ctx.Owner.Self;
        _player = ctx.Player;
        _state = ctx.StateContext;
    }

    public bool CanEnter()
    {
        if (_player == null) return false;

        // Attack中・Bark中はTurnを行わない（仕様: TurnBehaviour Constraints）
        if (_state == null) return false;
        return _state.CanMove();
    }

    public bool CanContinue()
    {
        return _player != null;
    }

    public void OnEnter()
    {
        IsFinished = false;
    }

    public void OnExit() { }

    public void Tick(float deltaTime)
    {
        // 上書き方向が設定されている場合はそちらを優先する
        Vector3 toTarget = _overrideDirection.HasValue
            ? _overrideDirection.Value
            : (_player != null ? _player.position - _self.position : Vector3.zero);

        toTarget.y = 0f;

        if (_player == null && !_overrideDirection.HasValue)
        {
            IsFinished = true;
            return;
        }

        if (toTarget.sqrMagnitude < 0.0001f)
        {
            IsFinished = true;
            return;
        }

        Quaternion targetRot = Quaternion.LookRotation(toTarget);
        float angle = Quaternion.Angle(_self.rotation, targetRot);

        if (angle < 0.5f)
        {
            _self.rotation = targetRot;
            IsFinished = true;
            return;
        }

        float t = Mathf.Clamp01(angle / _profile.MaxAngle);
        float turnSpeed = Mathf.Lerp(
            _profile.MinTurnSpeed,
            _profile.MaxTurnSpeed,
            t
        );

        _self.rotation = Quaternion.RotateTowards(
            _self.rotation,
            targetRot,
            turnSpeed * deltaTime
        );
    }

    private Transform _self;
    private Transform _player;
    private readonly TurnProfile _profile;
    // プレイヤー以外の方向を向かせる場合の上書きベクトル
    private Vector3? _overrideDirection;
    private EnemyStateContext _state;
}
