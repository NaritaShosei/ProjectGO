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
        _weaponSwingFired = false;

        if (animator.TryGetComponent(out IEnemyAnimationController controller))
        {
            controller.AnimEvent_AttackStart();
        }
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (animator.speed == 0f) return;
        float currentTime = stateInfo.normalizedTime * stateInfo.length;
        float normalizedTime = stateInfo.normalizedTime % 1f;
        // 武器スイングSE
        if (!_weaponSwingFired && normalizedTime >= _weaponSwingTime)
        {
            _weaponSwingFired = true;

            if (animator.TryGetComponent(out IEnemyAnimationController controller))
            {
                controller.AnimEvent_WeaponSwing();
            }
        }

        if (_attackHitFired) return;

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

    [SerializeField, Tooltip("武器を振るSEの発火タイミング(秒数)")]
    private float _weaponSwingTime = 0.633f;//MobEnemyは 0.32f Golemは0.6533f

    private bool _attackHitFired;
    private bool _weaponSwingFired;
}
