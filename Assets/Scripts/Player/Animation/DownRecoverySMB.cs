using UnityEngine;

/// <summary>
/// ダウン回復アニメーション終了時に呼ばれる
/// </summary>
public class DownRecoverySMB : StateMachineBehaviour
{
    /// <summary>
    /// アニメーションの進行度をPlayerAnimationControllerに通知する
    /// </summary>
    public override void OnStateUpdate(
    Animator animator,
    AnimatorStateInfo stateInfo,
    int layerIndex)
    {
        var controller =
            animator.GetComponentInParent<PlayerAnimationController>();

        controller?.AnimEvent_DownRecoveryProgress(
            stateInfo.normalizedTime
        );
    }

    /// <summary>
    /// アニメーション終了時にPlayerAnimationControllerに通知する
    /// </summary>
    public override void OnStateExit(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        var controller =
            animator.GetComponentInParent<PlayerAnimationController>();

        controller?.AnimEvent_DownRecoveryEnd();
    }
}
