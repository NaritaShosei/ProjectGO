using UnityEngine;

public class EnemySpawnSMB : MonoBehaviour
{
    public override void OnStateEnter(Animator animator,AnimatorStateInfo stateInfo,int layerIndex)
    {
        if (animator.TryGetComponent(out IEnemyAnimationController controller))
        {
            controller.AnimEvent_SpawnStart();
        }
    }
}
