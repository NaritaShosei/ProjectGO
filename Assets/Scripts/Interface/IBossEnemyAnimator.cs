using BossEnemy.Enum;
using BossEnemy.Interface;
using System;
using UnityEngine;

public interface IBossEnemyAnimator
{
    /// <summary>攻撃ヒットタイミングのイベント</summary>
    public event Action OnAttackHit;
    /// <summary>攻撃アニメーション終了のイベント</summary>
    public event Action OnAttackEnd;
    /// <summary>死亡アニメーション終了のイベント</summary>
    public event Action OnDeadEnd;

    /// <summary>
    /// 移動速度を設定する（Idle / Move の切り替えに使用）
    /// </summary>
    public void SetSpeed(float xSpeed, float zSpeed);

    /// <summary>
    /// 攻撃中フラグを設定する
    /// </summary>
    public void SetAttacking(bool value, int attackDataID);

    /// <summary>
    /// 各所アーマー破壊フラグを設定する
    /// </summary>
    public void SetPosture(PostureType postureType);

    /// <summary>
    /// 感電フラグを設定する
    /// </summary>
    public void SetElectrified(bool value);

    /// <summary>
    /// 死亡フラグを設定する（一度設定したら戻さない）
    /// </summary>
    public void SetDead();

    /// <summary>
    /// Phase切り替え処理
    /// </summary>
    public void SetPhaseChange(int nextPhase);

    /// <summary>
    /// アニメーション再生速度を設定する。
    /// HitStopManager から OnSpeedChange 経由で呼ばれる。
    /// </summary>
    public void SetAnimSpeed(float speed);

    /// <summary>イベント購読を解除する</summary>
    public void Dispose();
}
