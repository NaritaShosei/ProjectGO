using System;
using UnityEngine;

/// <summary>
/// AnimationClipのAnimationEventを受け取り、外部へ中継する薄いクラス。
/// EnemyのAnimatorと同じGameObjectにアタッチする。
/// ロジックは持たず、イベントの中継のみを担当する。
/// </summary>
public class EnemyAnimationEventReceiver : MonoBehaviour
{
    /// <summary>攻撃ヒットタイミングのイベント</summary>
    public event Action OnAttackHit;

    /// <summary>攻撃アニメーション終了のイベント</summary>
    public event Action OnAttackEnd;

    /// <summary>Barkアニメーション終了のイベント</summary>
    public event Action OnBarkEnd;

    /// <summary>GetUpアニメーション終了のイベント</summary>
    public event Action OnGetUpEnd;

    /// <summary>死亡アニメーション終了のイベント</summary>
    public event Action OnDeadEnd;

    /// <summary>
    /// AnimationClipから直接呼ばれるメソッド
    /// </summary>
    public void AnimEvent_AttackHit()
    {
        OnAttackHit?.Invoke();
    }

    /// <summary>
    /// AnimationClipから直接呼ばれるメソッド
    /// </summary>
    public void AnimEvent_AttackEnd()
    {
        OnAttackEnd?.Invoke();
    }

    /// <summary>
    /// AnimationClipから直接呼ばれるメソッド
    /// </summary>
    public void AnimEvent_BarkEnd()
    {
        OnBarkEnd?.Invoke();
    }

    /// <summary>
    /// AnimationClipから直接呼ばれるメソッド
    /// </summary>
    public void AnimEvent_GetUpEnd()
    {
        OnGetUpEnd?.Invoke();
    }

    /// <summary>
    /// AnimationClipから直接呼ばれるメソッド
    /// </summary>
    public void AnimEvent_DeadEnd()
    {
        OnDeadEnd?.Invoke();
    }
}
