using UnityEngine;

/// <summary>
/// DamagedステートにアタッチするSMB。
/// ステート終了時にDamagedEndを発火してPlayerAnimationControllerへ通知する。
/// </summary>
public class DamagedSMB : StateMachineBehaviour
{
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        PlayerAnimationController controller = animator.GetComponentInParent<PlayerAnimationController>();
        if (controller != null)
            controller.AnimEvent_DamagedEnd();
    }
}
