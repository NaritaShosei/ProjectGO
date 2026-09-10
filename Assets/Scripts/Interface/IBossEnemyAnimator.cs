using UnityEngine;
using System;

public interface IBossEnemyAnimator
{
    /// <summary>攻撃ヒットタイミングのイベント</summary>
    public event Action OnAttackHit;
    /// <summary>攻撃アニメーション終了のイベント</summary>
    public event Action OnAttackEnd;
    /// <summary>死亡アニメーション終了のイベント</summary>
    public event Action OnDeadEnd;

    /// <summary>移動速度を設定する（Idle / Move の切り替えに使用）</summary>
    public void SetSpeed(float xSpeed, float zSpeed);
    /// <summary>攻撃中フラグを設定する</summary>
    public void SetAttacking(bool value, string triggerValue);
    /// <summary>感電フラグを設定する</summary>
    public void SetElectrified(bool value);
    /// <summary>死亡フラグを設定する（一度設定したら戻さない）</summary>
    public void SetDead();
    /// <summary>アニメーション再生速度を設定する（HitStop制御に使用）</summary>
    public void SetAnimSpeed(float speed);
    /// <summary>イベント購読を解除する</summary>
    public void Dispose();
}
