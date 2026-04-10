using UnityEngine;

/// <summary>
/// Dodge ステートにアタッチする SMB。
/// ステート終了時に DodgeEnd を発火して PlayerAnimationController へ通知する。
/// </summary>
public class DodgeSMB : StateMachineBehaviour
{
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (animator.TryGetComponent(out PlayerAnimationController controller))
            controller.AnimEvent_DodgeEnd();
    }
}
