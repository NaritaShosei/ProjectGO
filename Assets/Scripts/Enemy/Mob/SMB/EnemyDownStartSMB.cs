using UnityEngine;

public class EnemyDownStartSMB : StateMachineBehaviour
{
    public override void OnStateEnter(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        if (animator.TryGetComponent(out IEnemyAnimationController c))
        {
            c.AnimEvent_DownStart();
        }
    }
}
