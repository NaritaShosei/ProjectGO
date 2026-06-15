using UnityEngine;

public class EnemyDownSMB : StateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetBool(_hashDownDone, false);
        _downEndFired = false;
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_downEndFired) return;
        if (animator.speed == 0f) return;

        if (stateInfo.normalizedTime >= 1f)
        {
            _downEndFired = true;

            animator.SetBool(_hashDownDone, true);

            if (animator.TryGetComponent(out IEnemyAnimationController c))
            {
                c.AnimEvent_DownEnd();
            }
        }
    }

    public override void OnStateExit(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        animator.SetBool(_hashDownDone, false);
    }

    private static readonly int _hashDownDone =
       Animator.StringToHash("DownDone");

    private bool _downEndFired;
}
