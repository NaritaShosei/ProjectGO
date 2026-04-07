using UnityEngine;

/// <summary>
/// DamagedステートにアタッチするSMB。
/// ステート終了時にDamagedEndを発火してPlayerAnimationControllerへ通知する。
/// </summary>
public class DamagedSMB : StateMachineBehaviour
{
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (animator.TryGetComponent(out PlayerAnimationController controller))
            controller.AnimEvent_DamagedEnd();
    }
}
