using UnityEngine;

/// <summary>
/// EnemyのAnimatorパラメータを一元管理するクラス
/// 文字列ハッシュで管理することでパフォーマンスを最適化する
/// </summary>
public class EnemyAnimator
{
    // Animatorパラメータのハッシュ
    private static readonly int _hashSpeed = Animator.StringToHash("Speed");
    private static readonly int _hashIsAttacking = Animator.StringToHash("IsAttacking");
    private static readonly int _hashIsBarking = Animator.StringToHash("IsBarking");
    private static readonly int _hashIsKnockback = Animator.StringToHash("IsKnockback");
    private static readonly int _hashIsElectrified = Animator.StringToHash("IsElectrified");
    private static readonly int _hashIsDead = Animator.StringToHash("IsDead");

    private readonly Animator _animator;

    public EnemyAnimator(Animator animator)
    {
        _animator = animator;
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
    /// ノックバックフラグを設定する
    /// </summary>
    public void SetKnockback(bool value)
    {
        if (_animator == null) return;
        _animator.SetBool(_hashIsKnockback, value);
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
}
