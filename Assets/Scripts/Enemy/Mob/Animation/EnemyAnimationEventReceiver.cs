using System;
using UnityEngine;

/// <summary>
/// アニメーションのイベントを受け取り、外部へ中継する薄いクラス。
/// EnemyのAnimatorと同じGameObjectにアタッチする。
/// ロジックは持たず、イベントの中継のみを担当する。
/// IEnemyAnimationControllerを実装し、SMBからanimator.TryGetComponent()で取得される。
/// </summary>
public class EnemyAnimationEventReceiver : MonoBehaviour, IEnemyAnimationController
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

    public event Action OnDownEnd;

    /// <summary>Downスタート時のイベント</summary>
    public event Action OnDownStart;
    /// <summary>攻撃エフェクト発火タイミングのイベント</summary>
    public event Action OnAttackEffect;
    /// <summary>歩行時のイベント</summary>
    public event Action OnFootstep;
    /// <summary>Barkアニメーション開始</summary>
    public event Action OnBarkStart;
    /// <summary>攻撃開始イベント</summary>
    public event Action OnAttackStart;
    /// <summary>武器スイングSEイベント</summary>
    public event Action OnWeaponSwing;
    /// <summary>エネミーのスポーンエフェクトイベント</summary>
    public event Action OnSpawnEffect;
    /// <summary>エネミーのスポーン終了イベント</summary>
    public event Action OnSpawnEnd;

    /// <summary>EnemyAttackSMB から攻撃ヒットタイミングで呼ばれる</summary>
    public void AnimEvent_AttackHit()
    {
        OnAttackHit?.Invoke();
    }

    /// <summary>EnemyAttackSMB からステート終了時に呼ばれる</summary>
    public void AnimEvent_AttackEnd()
    {
        OnAttackEnd?.Invoke();
    }

    /// <summary>EnemyBarkSMB からステート終了時に呼ばれる</summary>
    public void AnimEvent_BarkEnd()
    {
        OnBarkEnd?.Invoke();
    }

    /// <summary>EnemyGetUpSMB からステート終了時に呼ばれる</summary>
    public void AnimEvent_GetUpEnd()
    {
        OnGetUpEnd?.Invoke();
    }

    /// <summary>EnemyKnockbackSMB からステート終了時に呼ばれる</summary>
    public void AnimEvent_KnockbackEnd()
    {
        OnKnockbackEnd?.Invoke();
    }

    /// <summary>EnemyDeadSMB からステート終了時に呼ばれる</summary>
    public void AnimEvent_DeadEnd()
    {
        OnDeadEnd?.Invoke();
    }
    public void AnimEvent_DownEnd()
    {
        OnDownEnd?.Invoke();
    }

    public void AnimEvent_DownStart()
    {
        OnDownStart?.Invoke();
    }

    public void AnimEvent_AttackEffect()
    {
        OnAttackEffect?.Invoke();
    }
    public void AnimEvent_Footstep()
    {
        OnFootstep?.Invoke();
    }
    public void AnimEvent_BarkStart()
    {
        OnBarkStart?.Invoke();
    }

    public void AnimEvent_AttackStart()
    {
        OnAttackStart?.Invoke();
    }

    public void AnimEvent_WeaponSwing()
    {
        OnWeaponSwing?.Invoke();
    }

    public void AnimEvent_SpawnEffect()
    {
        OnSpawnEffect?.Invoke();
    }

    public void AnimEvent_SpawnEnd()
    {
        OnSpawnEnd?.Invoke();
    }
}
