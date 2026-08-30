using System;

/// <summary>
/// EnemyのAnimatorパラメータを管理するインターフェース
/// </summary>
public interface IEnemyAnimator
{
    /// <summary>攻撃ヒットタイミングのイベント</summary>
    public event Action OnAttackHit;
    /// <summary>攻撃アニメーション終了のイベント</summary>
    public event Action OnAttackEnd;
    /// <summary>Barkアニメーション終了のイベント</summary>
    public event Action OnBarkEnd;
    /// <summary>GetUpアニメーション終了のイベント</summary>
    public event Action OnGetUpEnd;
    /// <summary>ノックバック（Hit/Small）アニメーション終了のイベント</summary>
    public event Action OnKnockbackEnd;
    /// <summary>死亡アニメーション終了のイベント</summary>
    public event Action OnDeadEnd;
    /// <summary>ダウンアニメーション終了のイベント </summary>
    public event Action OnDownEnd;
    /// <summary>ダウン開始イベント</summary>
    public event Action OnDownStart;
    /// <summary>攻撃エフェクト・SE発火イベント</summary>
    public event Action OnAttackEffect;
    /// <summary>歩行時のイベント</summary>
    public event Action OnFootstep;
    /// <summary>Bark開始イベント</summary>
    event Action OnBarkStart;
    /// <summary>攻撃開始イベント</summary>
    event Action OnAttackStart;
    /// <summary>武器スイングSEイベント</summary>
    public event Action OnWeaponSwing;
    /// <summary>エネミーのスポーンエフェクト</summary>
    public event Action OnSpawnEffect;
    /// <summary>スポーンアニメーション終了イベント</summary>
    public event Action OnSpawnEnd;
    /// <summary>盾破壊開始イベント</summary>
    public event Action OnShieldBreakStart;

    public event Action OnShieldBlockHitStart;

    /// <summary>移動速度を設定する（Idle / Move の切り替えに使用）</summary>
    public void SetSpeed(float speed);
    /// <summary>攻撃中フラグを設定する</summary>
    public void SetAttacking(bool value);
    /// <summary>Barkフラグを設定する</summary>
    public void SetBarking(bool value);
    /// <summary>ノックバックフラグを設定する</summary>
    public void SetKnockback(bool value, KnockbackLevel level = KnockbackLevel.Hit);
    /// <summary>感電フラグを設定する</summary>
    public void SetElectrified(bool value);
    /// <summary>死亡フラグを設定する（一度設定したら戻さない）</summary>
    public void SetDead();
    /// <summary>アニメーション再生速度を設定する（HitStop制御に使用）</summary>
    public void SetAnimSpeed(float speed);
    /// <summary>イベント購読を解除する</summary>
    public void Dispose();
    /// <summary> </summary><param name="value"></param>
    public void SetDown(bool value);

    public void DownTrigger();
    public void ShieldBreakTrigger();
    public void ShieldBlockHitTrigger();
}

/// <summary>
/// AnimatorをもたないEnemy実装（BossCore / EnemyArmer）向けのNull Objectパターン実装。
/// 全操作は無操作となり、呼び出し側のnullチェックを不要にする。
/// </summary>
public sealed class NullEnemyAnimator : IEnemyAnimator
{
#pragma warning disable CS0067
    public event Action OnAttackHit;
    public event Action OnAttackEnd;
    public event Action OnBarkEnd;
    public event Action OnGetUpEnd;
    public event Action OnKnockbackEnd;
    public event Action OnDeadEnd;
    public event Action OnDownEnd;
    public event Action OnDownStart;
    public event Action OnAttackEffect;
    public event Action OnFootstep;
    public event Action OnBarkStart;
    public event Action OnAttackStart;
    public event Action OnWeaponSwing;
    public event Action OnSpawnEffect;
    public event Action OnSpawnEnd;
    public event Action OnShieldBreakStart;
    public event Action OnShieldBlockHitStart;
#pragma warning restore CS0067

    public void SetSpeed(float speed) { }
    public void SetAttacking(bool value) { }
    public void SetBarking(bool value) { }
    public void SetKnockback(bool value, KnockbackLevel level = KnockbackLevel.Hit) { }
    public void SetElectrified(bool value) { }
    public void SetDead() { }
    public void SetAnimSpeed(float speed) { }
    public void Dispose() { }
    public void SetDown(bool value){ }
    public void DownTrigger() { }
    public void ShieldBreakTrigger() { }
    public void ShieldBlockHitTrigger() { }
}
