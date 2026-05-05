using UnityEngine;

/// <summary>
/// DeadステートにアタッチするSMB。
/// ステート終了時にDeadEndを発火する。
/// </summary>
public class EnemyDeadSMB : StateMachineBehaviour
{
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (animator.TryGetComponent(out IEnemyAnimationController controller))
            controller.AnimEvent_DeadEnd();
    }
}
