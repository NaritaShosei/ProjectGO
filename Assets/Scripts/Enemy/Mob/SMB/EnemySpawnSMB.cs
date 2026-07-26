using UnityEngine;

public class EnemySpawnSMB : StateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (animator.TryGetComponent(out IEnemyAnimationController controller))
        {
            controller.AnimEvent_SpawnEffect();
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (animator.TryGetComponent(out IEnemyAnimationController controller))
        {
            controller.AnimEvent_SpawnEnd();
        }
    }
}
