using UnityEngine;

public class EnemyShieldBreakSMB : StateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (animator.TryGetComponent(
                   out IEnemyAnimationController controller))
        {
            controller.AnimEvent_ShieldBreakStart();
        }

    }
}
