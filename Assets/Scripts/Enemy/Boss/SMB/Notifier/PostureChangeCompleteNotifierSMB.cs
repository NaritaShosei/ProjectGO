using UnityEngine;

public class PostureChangeCompleteNotifierSMB : BossCharacterSMB
{
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _animationEventReceiver.AnimEvent_PostureChangeCompleted();
    }
}

