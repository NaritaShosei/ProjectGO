using UnityEngine;

public class EnemySpawnSMB : StateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _firstFired = false;
        _secondFired = false;
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        var normalizedTime = stateInfo.normalizedTime;

        if (!_firstFired && normalizedTime >= _firstEffectTiming)
        {
            _firstFired = true;
            Fire(animator);
        }

        if (!_secondFired && normalizedTime >= _secondEffectTiming)
        {
            _secondFired = true;
            Fire(animator);
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (animator.TryGetComponent(out IEnemyAnimationController controller))
        {
            controller.AnimEvent_SpawnEnd();
        }
    }

    [Header("Effect Timing (normalized 0-1)")]
    [SerializeField, Range(0f, 1f)] private float _firstEffectTiming = 0f;
    [SerializeField, Range(0f, 1f)] private float _secondEffectTiming = 0.6f;

    private bool _firstFired;
    private bool _secondFired;

    private void Fire(Animator animator)
    {
        if (animator.TryGetComponent(out IEnemyAnimationController controller))
        {
            controller.AnimEvent_SpawnEffect();
        }
    }
}
