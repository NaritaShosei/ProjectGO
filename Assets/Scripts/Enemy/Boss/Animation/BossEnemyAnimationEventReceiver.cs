using JetBrains.Annotations;
using System;
using UnityEngine;

/// <summary>
/// アニメーションのイベントを受け取り、外部へ中継する薄いクラス。
/// BossEnemyのAnimatorと同じGameObjectにアタッチする。
/// ロジックは持たず、イベントの中継のみを担当する。
/// IEnemyAnimationControllerを継承したIBossEnemyAnimationControllerを実装し、SMBからanimator.TryGetComponent()で取得される。
/// </summary>

public class BossEnemyAnimationEventReceiver
{
    /// <summary>Animation中に動く際のイベント(目的地と到達までの時間)</summary>
    public event Action<Vector3, float> OnMove;

    /// <summary>BossEnemyのIsTriggerのOnOffを切り替えるイベント</summary>
    public event Action<bool> OnColliderIsTriggerIsEnabled;

    /// <summary>攻撃ヒットタイミングのイベント</summary>
    public event Action OnAttackHit;

    /// <summary>攻撃アニメーション終了のイベント</summary>
    public event Action OnAttackEnd;

    /// <summary>Phase切り替え終了のイベント</summary>
    public event Action OnPhaseChangeEnd;

    /// <summary>死亡アニメーション終了のイベント</summary>
    public event Action OnDeadEnd;

    /// <summary>AttackSMB から移動開始タイミングで呼ばれる</summary>
    public void AnimEvent_Move(Vector3 goal, float time)
    {
        OnMove?.Invoke(goal, time);
    }

    /// <summary> BossEnemyのIsTriggerのOnOffを切り替える </summary>
    public void AnimEvent_ColliderIsTriggerIsEnabled(bool isTrigger)
    {
        OnColliderIsTriggerIsEnabled?.Invoke(isTrigger);
    }

    /// <summary>AttackSMB から攻撃ヒットタイミングで呼ばれる</summary>
    public void AnimEvent_AttackHit()
    {
        OnAttackHit?.Invoke();
    }

    /// <summary>AttackSMB からステート終了時に呼ばれる</summary>
    public void AnimEvent_AttackEnd()
    {
        OnAttackEnd?.Invoke();
    }

    /// <summary>PhaseChangeSMB からステート終了時に呼ばれる</summary>
    public void AnimEvent_PhaseChangeEnd()
    {
        OnPhaseChangeEnd?.Invoke();
    }

    /// <summary>DeadSMB からステート終了時に呼ばれる</summary>
    public void AnimEvent_DeadEnd()
    {
        OnDeadEnd?.Invoke();
    }
}
