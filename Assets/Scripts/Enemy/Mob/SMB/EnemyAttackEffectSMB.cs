using UnityEngine;

public class EnemyAttackEffectSMB : StateMachineBehaviour
{
    [Header("Timings (seconds)")]
    [SerializeField]
    private float _effectTiming = 0.2f;

    private bool _effectFired; 

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
}
