using UnityEngine;

public class ChargeReadySMB : StateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _fired = false;
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (stateInfo.normalizedTime >= 0.99f)
        {
            FireChargeReady(animator);
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        FireChargeReady(animator);
    }

    private bool _fired;

    private void FireChargeReady(Animator animator)
    {
        if (_fired)
            return;

        _fired = true;

        if (animator.TryGetComponent(out PlayerAnimationController controller))
            controller.AnimEvent_ChargeReady();
    }
}
