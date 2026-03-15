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
        _player = player;
    }

    public bool CanEnter()
    {
        return _player != null;
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
        if (_player == null)
        {
            IsFinished = true;
            return;
        }

        Vector3 toTarget = _player.position - _self.position;
        toTarget.y = 0f;

        // プレイヤーとの距離が近すぎる場合はスキップ
        if (toTarget.sqrMagnitude < 0.0001f)
        {
            IsFinished = true;
            return;
        }

        Quaternion targetRot = Quaternion.LookRotation(toTarget);
        float angle = Quaternion.Angle(_self.rotation, targetRot);

        // 誤差範囲内であれば即座に向きを合わせて終了
        if (angle < 0.5f)
        {
            _self.rotation = targetRot;
            IsFinished = true;
            return;
        }

        // 角度に応じて回転速度を変化させる
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
}
