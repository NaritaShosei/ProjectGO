using UnityEngine;

public class ChargeReadySMB : StateMachineBehaviour
{
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (animator.TryGetComponent(out PlayerAnimationController controller))
            controller.AnimEvent_ChargeReady();
    }
}
