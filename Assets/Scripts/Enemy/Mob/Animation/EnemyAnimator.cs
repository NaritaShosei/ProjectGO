using UnityEngine;
using System;

/// <summary>
/// EnemyのAnimatorパラメータを一元管理するクラス
/// 文字列ハッシュで管理することでパフォーマンスを最適化する
/// </summary>
public class EnemyAnimator : IEnemyAnimator
{
    // Receiverから中継するイベント（外部への単一エントリポイント）
    public event Action OnAttackHit;
    public event Action OnAttackEnd;
    public event Action OnBarkEnd;
    public event Action OnGetUpEnd;
    public event Action OnKnockbackEnd;
    public event Action OnDeadEnd;
    public event Action OnDownEnd;
    public event Action OnAttackEffect;
    public event Action OnDownStart;
    public event Action OnFootstep;
    public event Action OnBarkStart;

    /// <summary>
    /// コンストラクタ。ReceiverのイベントをEnemyAnimatorへ中継する。
    /// </summary>
    public EnemyAnimator(Animator animator, EnemyAnimationEventReceiver receiver)
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
        _receiver.OnDownEnd += HandleDownEnd;
        _receiver.OnDownStart += HandleDownStart;
        _receiver.OnAttackEffect += HandleAttackEffect;
        _receiver.OnFootstep += HandleFootstep;
        _receiver.OnBarkStart += HandleBarkStart;
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
        _receiver.OnDownEnd -= HandleDownEnd;
        _receiver.OnDownStart -= HandleDownStart;
        _receiver.OnAttackEffect -= HandleAttackEffect;
        _receiver.OnFootstep -= HandleFootstep;
        _receiver.OnBarkStart -= HandleBarkStart;
    }

    public void SetDown(bool value)
    {
        if (_animator == null) return;
        _animator.SetBool(_hashIsDown, value);
    }

    public void DownTrigger()
    {
        if (_animator == null) return;
        _animator.SetTrigger(_hashDownTrigger);
    }

    // Animatorパラメータのハッシュ
    private static readonly int _hashSpeed = Animator.StringToHash("Speed");
    private static readonly int _hashIsAttacking = Animator.StringToHash("IsAttacking");
    private static readonly int _hashIsBarking = Animator.StringToHash("IsBarking");
    private static readonly int _hashIsElectrified = Animator.StringToHash("IsElectrified");
    private static readonly int _hashIsDead = Animator.StringToHash("IsDead");

    private static readonly int _hashIsKnockback = Animator.StringToHash("IsKnockback");
    private static readonly int _hashKnockbackLevel = Animator.StringToHash("KnockbackLevel");
    private static readonly int _hashIsDown = Animator.StringToHash("IsDown");
    private static readonly int _hashDownTrigger = Animator.StringToHash("DownTrigger");

    private readonly Animator _animator;

    // 購読解除のためにReceiverを保持する
    private readonly EnemyAnimationEventReceiver _receiver;

    private void HandleAttackHit() => OnAttackHit?.Invoke();
    private void HandleAttackEnd() => OnAttackEnd?.Invoke();
    private void HandleBarkEnd() => OnBarkEnd?.Invoke();
    private void HandleGetUpEnd() => OnGetUpEnd?.Invoke();
    private void HandleKnockbackEnd() => OnKnockbackEnd?.Invoke();
    private void HandleDeadEnd() => OnDeadEnd?.Invoke();
    private void HandleDownEnd() => OnDownEnd?.Invoke();
    private void HandleDownStart() => OnDownStart?.Invoke();
    private void HandleAttackEffect() => OnAttackEffect?.Invoke();
    private void HandleFootstep() => OnFootstep?.Invoke();
    private void HandleBarkStart() => OnBarkStart?.Invoke();
}
