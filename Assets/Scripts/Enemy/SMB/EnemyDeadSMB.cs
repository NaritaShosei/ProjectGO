using UnityEngine;

/// <summary>
/// DeadステートにアタッチするSMB。
/// ステート終了時にDeadEndを発火する。
/// </summary>
public class EnemyDeadSMB : StateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.TryGetComponent(out _controller);
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_controller == null) return;

        _controller.AnimEvent_DeadEnd();
    }

    private IEnemyAnimationController _controller;
}
