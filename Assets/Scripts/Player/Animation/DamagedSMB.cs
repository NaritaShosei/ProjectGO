using UnityEngine;

/// <summary>
/// DamagedステートにアタッチするSMB。
/// ステート終了時にDamagedEndを発火してPlayerAnimationControllerへ通知する。
/// </summary>
public class DamagedSMB : StateMachineBehaviour
{
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // モード変更などで復帰遷移を逃しても、再生済みのリアクションに留まらない。
        // 通常の遷移中はOnStateExitに任せ、ブレンドを途中で上書きしない。
        if (stateInfo.normalizedTime < 1f || animator.IsInTransition(layerIndex)) return;

        PlayerAnimationController controller = animator.GetComponentInParent<PlayerAnimationController>();
        if (controller != null)
            controller.RecoverCompletedDamageReaction();
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        PlayerAnimationController controller = animator.GetComponentInParent<PlayerAnimationController>();
        if (controller != null)
            controller.AnimEvent_DamagedEnd();
    }
}
