using UnityEngine;

/// <summary>
/// AttackステートにアタッチするSMB。
/// 指定した秒数でAttackHitを発火し、ステート終了時にAttackEndを発火する。
/// Player側のAttackEventSMBに相当するEnemy版。
/// </summary>
public class EnemyAttackSMB : StateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _attackHitFired = false;
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (animator.speed == 0f) return;
        if (_attackHitFired) return;

        float currentTime = stateInfo.normalizedTime * stateInfo.length;
        if (currentTime >= _attackHitTime)
        {
            _attackHitFired = true;
            if (animator.TryGetComponent(out IEnemyAnimationController controller))
                controller.AnimEvent_AttackHit();
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (animator.TryGetComponent(out IEnemyAnimationController controller))
            controller.AnimEvent_AttackEnd();
    }

    [Header("Timings (seconds)")]
    [Tooltip("攻撃ヒット判定を発火する秒数")]
    [SerializeField] private float _attackHitTime = 0.3f;

    private bool _attackHitFired;
}
