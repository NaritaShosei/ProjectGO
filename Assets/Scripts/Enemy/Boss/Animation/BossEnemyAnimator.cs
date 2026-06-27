using UnityEngine;
using System;

public class BossEnemyAnimator : IBossEnemyAnimator
{
    // Receiverから中継するイベント（外部への単一エントリポイント）
    public event Action OnAttackHit;
    public event Action OnAttackEnd;
    public event Action OnDeadEnd;
    public event Action OnPhaseChangeEnd;

    /// <summary>
    /// コンストラクタ。ReceiverのイベントをEnemyAnimatorへ中継する。
    /// </summary>
    public BossEnemyAnimator(Animator animator, BossEnemyAnimationEventReceiver receiver)
    {
        _animator = animator;
        _receiver = receiver;

        if (_receiver == null) return;

        _receiver.OnAttackHit += HandleAttackHit;
        _receiver.OnAttackEnd += HandleAttackEnd;
        _receiver.OnDeadEnd += HandleDeadEnd;
        _receiver.OnPhaseChangeEnd += HandlePhaseChangeEnd;
    }

    /// <summary>
    /// 移動速度を設定する（Idle / Move の切り替えに使用）
    /// </summary>
    public void SetSpeed(float xSpeed, float zSpeed)
    {
        if (_animator == null) return;
        _animator.SetFloat(_hashXSpeed, xSpeed);
        _animator.SetFloat(_hashZSpeed, zSpeed);
    }

    /// <summary>
    /// 攻撃中フラグを設定する
    /// </summary>
    public void SetAttacking(bool value, string triggerValue = null)
    {
        if (_animator == null) return;
            _animator.SetBool(_hashIsAttacking, value);

        Debug.Log("攻撃のトリガー" + triggerValue);

        if(triggerValue != null) 
            _animator.SetTrigger(triggerValue);
    }

    /// <summary>
    /// 感電フラグを設定する
    /// </summary>
    public void SetElectrified(bool value)
    {
        if (_animator == null) return;
        _animator.SetBool(_hashIsElectrified, value);
    }

    /// <summary>
    /// 死亡フラグを設定する（一度設定したら戻さない）
    /// </summary>
    public void SetDead()
    {
        if (_animator == null) return;
        _animator.SetBool(_hashIsDead, true);
    }

    public void SetPhaseChange()
    {
        if (_animator == null) return;
        _animator.SetTrigger(_hashPhaseChange);
    }

    /// <summary>
    /// アニメーション再生速度を設定する。
    /// HitStopManager から OnSpeedChange 経由で呼ばれる。
    /// </summary>
    public void SetAnimSpeed(float speed)
    {
        if (_animator == null) return;
        _animator.speed = speed;
    }

    /// <summary>
    /// Receiverのイベント購読を解除する。
    /// EnemyのOnDestroyから呼ぶこと。
    /// </summary>
    public void Dispose()
    {
        if (_receiver == null) return;

        _receiver.OnAttackHit -= HandleAttackHit;
        _receiver.OnAttackEnd -= HandleAttackEnd;
        _receiver.OnDeadEnd -= HandleDeadEnd;
        _receiver.OnPhaseChangeEnd -= HandlePhaseChangeEnd;
    }

    // Animatorパラメータのハッシュ
    private readonly int _hashCurrentHP = Animator.StringToHash("CurrentHP");
    private readonly int _hashXSpeed = Animator.StringToHash("Speed_x");
    private readonly int _hashZSpeed = Animator.StringToHash("Speed_z");
    private readonly int _hashIsAttacking = Animator.StringToHash("IsAttacking");
    private readonly int _hashIsElectrified = Animator.StringToHash("IsElectrified");
    private readonly int _hashIsDead = Animator.StringToHash("IsDead");
    private readonly int _hashPhaseChange = Animator.StringToHash("PhaseChangeTrigger");

    private readonly Animator _animator;

    // 購読解除のためにReceiverを保持する
    private readonly BossEnemyAnimationEventReceiver _receiver;

    private void HandleAttackHit() => OnAttackHit?.Invoke();
    private void HandleAttackEnd() => OnAttackEnd?.Invoke();
    private void HandleDeadEnd() => OnDeadEnd?.Invoke();
    private void HandlePhaseChangeEnd() => OnPhaseChangeEnd?.Invoke();
}
