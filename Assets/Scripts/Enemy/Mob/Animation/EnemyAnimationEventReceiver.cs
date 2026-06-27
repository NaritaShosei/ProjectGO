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
}
