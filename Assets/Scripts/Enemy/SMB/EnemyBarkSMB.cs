using UnityEngine;

/// <summary>
/// BarkステートにアタッチするSMB。
/// ステート終了時にBarkEndを発火する。
/// </summary>
public class EnemyBarkSMB : StateMachineBehaviour
{
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (animator.TryGetComponent(out IEnemyAnimationController controller))
            controller.AnimEvent_BarkEnd();
    }
}
