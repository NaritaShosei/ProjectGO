using UnityEngine;

/// <summary>
/// BarkステートにアタッチするSMB。
/// ステート終了時にBarkEndを発火する。
/// </summary>
public class EnemyBarkSMB : StateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.TryGetComponent(out _controller);
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_controller == null) return;

        _controller.AnimEvent_BarkEnd();
    }

    private IEnemyAnimationController _controller;
}
