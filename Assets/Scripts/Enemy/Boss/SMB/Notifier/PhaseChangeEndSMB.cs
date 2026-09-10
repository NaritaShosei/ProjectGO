using UnityEngine;

public class PhaseChangeEndSMB : BossCharacterSMB
{
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateExit(animator, stateInfo, layerIndex);

        _animationEventReceiver?.AnimEvent_PhaseChangeEnd();
    }
}
