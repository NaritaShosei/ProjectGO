using UnityEngine;

/// <summary>
/// Knockback_Hit / Knockback_Small / Knockback_Large ステートにアタッチするSMB。
/// OnStateEnter: IsKnockback をリセットして同一ステートへの再トリガーを防ぐ。
/// OnStateExit : KnockbackEnd を通知する（Hit/Small の条件終了に使用）。
///               Large は KnockbackCondition 側で無視し、GetUpEnd を待つ。
/// BlocksAction は KnockbackCondition 側で管理するため、
/// このリセットで行動ブロックが解除されることはない。
/// </summary>
public class EnemyKnockbackSMB : StateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetBool(_hashIsKnockback, false);
        animator.SetInteger(_hashKnockbackLevel, 0);
        animator.TryGetComponent(out _controller);
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_controller == null) return;
        _controller.AnimEvent_KnockbackEnd();
    }

    private static readonly int _hashIsKnockback = Animator.StringToHash("IsKnockback");
    private static readonly int _hashKnockbackLevel = Animator.StringToHash("KnockbackLevel");

    private IEnemyAnimationController _controller;
}
