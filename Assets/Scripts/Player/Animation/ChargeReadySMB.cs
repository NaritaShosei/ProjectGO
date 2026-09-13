using UnityEngine;

public class ChargeReadySMB : StateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _fired = false;
        animator.TryGetComponent(out _controller);
        _animationVersion = _controller != null ? _controller.CombatAnimationVersion : -1;
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
    private PlayerAnimationController _controller;
    private int _animationVersion;

    private void FireChargeReady(Animator animator)
    {
        if (_fired || _controller == null
            || _animationVersion != _controller.CombatAnimationVersion)
            return;

        _fired = true;

        if (animator.TryGetComponent(out PlayerAnimationController controller))
            controller.AnimEvent_ChargeReady();
    }
}
