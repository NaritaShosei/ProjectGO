using UnityEngine;
using System;

public class BossEnemyAnimator : IBossEnemyAnimator
{
    // Receiverから中継するイベント（外部への単一エントリポイント）
    public event Action OnAttackHit;
    public event Action OnAttackEnd;
    public event Action OnBarkEnd;
    public event Action OnGetUpEnd;
    public event Action OnKnockbackEnd;
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
        _receiver.OnBarkEnd += HandleBarkEnd;
        _receiver.OnGetUpEnd += HandleGetUpEnd;
        _receiver.OnKnockbackEnd += HandleKnockbackEnd;
        _receiver.OnDeadEnd += HandleDeadEnd;
        _receiver.OnPhaseChangeEnd += HandlePhaseChangeEnd;
    }

    /// <summary>
    /// 移動速度を設定する（Idle / Move の切り替えに使用）
    /// </summary>
    public void SetSpeed(float speed)
    {
        if (_animator == null) return;
        _animator.SetFloat(_hashSpeed, speed);
    }

    /// <summary>
    /// 攻撃中フラグを設定する
    /// </summary>
    public void SetAttacking(bool value)
    {
        if (_animator == null) return;
        _animator.SetBool(_hashIsAttacking, value);
    }

    /// <summary>
    /// 複数の攻撃Animationの中から任意のAnimationフラグを立てる
    /// </summary>
    public void SetAttackTrigger(string triggerName)
    {
        if (_animator == null) return;
        _animator.SetTrigger(triggerName);
    }

    /// <summary>
    /// Barkフラグを設定する
    /// </summary>
    public void SetBarking(bool value)
    {
        if (_animator == null) return;
        _animator.SetBool(_hashIsBarking, value);
    }

    /// <summary>
    /// ノックバックフラグを設定する。
    /// value = true のとき KnockbackLevel も同時に書き込む。
    /// value = false のとき KnockbackLevel はリセットしない（仕様通り）。
    /// </summary>
    public void SetKnockback(bool value, KnockbackLevel level = KnockbackLevel.Hit)
    {
        if (_animator == null) return;
        _animator.SetBool(_hashIsKnockback, value);

        if (value)
        {
            _animator.SetInteger(_hashKnockbackLevel, (int)level);
        }
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
        _receiver.OnBarkEnd -= HandleBarkEnd;
        _receiver.OnGetUpEnd -= HandleGetUpEnd;
        _receiver.OnKnockbackEnd -= HandleKnockbackEnd;
        _receiver.OnDeadEnd -= HandleDeadEnd;
        _receiver.OnPhaseChangeEnd -= HandlePhaseChangeEnd;
    }

    // Animatorパラメータのハッシュ
    private static readonly int _hashSpeed = Animator.StringToHash("Speed");
    private static readonly int _hashIsAttacking = Animator.StringToHash("IsAttacking");
    private static readonly int _hashIsBarking = Animator.StringToHash("IsBarking");
    private static readonly int _hashIsElectrified = Animator.StringToHash("IsElectrified");
    private static readonly int _hashIsDead = Animator.StringToHash("IsDead");

    private static readonly int _hashIsKnockback = Animator.StringToHash("IsKnockback");
    private static readonly int _hashKnockbackLevel = Animator.StringToHash("KnockbackLevel");

    private readonly Animator _animator;

    // 購読解除のためにReceiverを保持する
    private readonly BossEnemyAnimationEventReceiver _receiver;

    private void HandleAttackHit() => OnAttackHit?.Invoke();
    private void HandleAttackEnd() => OnAttackEnd?.Invoke();
    private void HandleBarkEnd() => OnBarkEnd?.Invoke();
    private void HandleGetUpEnd() => OnGetUpEnd?.Invoke();
    private void HandleKnockbackEnd() => OnKnockbackEnd?.Invoke();
    private void HandleDeadEnd() => OnDeadEnd?.Invoke();
    private void HandlePhaseChangeEnd() => OnPhaseChangeEnd?.Invoke();
}
