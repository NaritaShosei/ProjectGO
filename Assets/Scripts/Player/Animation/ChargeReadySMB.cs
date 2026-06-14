using UnityEngine;

public class ChargeReadySMB : StateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _fired = false;
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_fired)
            return;

        if (stateInfo.normalizedTime >= 0.99f)
        {
            _fired = true;

            if (animator.TryGetComponent(out PlayerAnimationController controller))
                controller.AnimEvent_ChargeReady();
        }
    }

    private bool _fired;
}
