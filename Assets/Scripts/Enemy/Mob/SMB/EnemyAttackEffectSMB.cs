using UnityEngine;

public class EnemyAttackEffectSMB : StateMachineBehaviour
{
    public override void OnStateEnter (Animator animator, AnimatorStateInfo stateInfo,int layerIndex)
    {
        _effectFired = false;
    }

    public override void OnStateUpdate(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        if (_effectFired) return;

        if (stateInfo.normalizedTime <  _effectTiming)
        {
            return;
        }

        _effectFired = true;

        if (animator.TryGetComponent(
            out IEnemyAnimationController controller))
        {
            controller.AnimEvent_AttackEffect();
        }
    }

    [Header("Effect Timing (normalized 0-1)")]
    [SerializeField, Range(0f, 1f)]
    private float _effectTiming = 0.2f;

    private bool _effectFired;
}
