using UnityEngine;

public class PostureChangeCompleteNotifierSMB : BossCharacterSMB
{
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Debug.Log("姿勢が変更されました");
        _animationEventReceiver.AnimEvent_PostureChangeCompleted();
    }
}

