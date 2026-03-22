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
        // 毎回フラグをリセットする
        _attackHitFired = false;

        animator.TryGetComponent(out _controller);
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // アニメーションが停止している場合（HitStop中など）は処理しない
        if (animator.speed == 0f) return;

        if (_controller == null) return;

        float currentTime = stateInfo.normalizedTime * stateInfo.length;

        // 攻撃ヒットタイミングの発火（1ステートにつき1回のみ）
        if (!_attackHitFired && currentTime >= _attackHitTime)
        {
            _attackHitFired = true;
            _controller.AnimEvent_AttackHit();
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_controller == null) return;

        _controller.AnimEvent_AttackEnd();
    }

    [Header("Timings (seconds)")]
    [Tooltip("攻撃ヒット判定を発火する秒数")]
    [SerializeField] private float _attackHitTime = 0.3f;

    private IEnemyAnimationController _controller;
    private bool _attackHitFired;
}
