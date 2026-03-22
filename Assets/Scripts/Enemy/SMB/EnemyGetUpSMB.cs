using UnityEngine;

/// <summary>
/// GetUpステートにアタッチするSMB。
/// ステート終了時にGetUpEndを発火する。
/// </summary>
public class EnemyGetUpSMB : StateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.TryGetComponent(out _controller);
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_controller == null) return;

        _controller.AnimEvent_GetUpEnd();
    }

    private IEnemyAnimationController _controller;
}
