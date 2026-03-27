using UnityEngine;

/// <summary>
/// Knockback_Hit / Knockback_Small / Knockback_Large ステートにアタッチするSMB。
/// OnStateEnter : IsKnockback・KnockbackDone をリセットする。
/// OnStateUpdate: アニメーション自然完了（normalizedTime >= 1）を検出して KnockbackEnd を発火する。
///                HasExitTime の代替。連続被弾時の割り込みにも対応する。
/// OnStateExit  : KnockbackDone をリセットする。割り込み退場時は KnockbackEnd を発火しない。
/// </summary>
public class EnemyKnockbackSMB : StateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetBool(_hashIsKnockback, false);
        animator.SetInteger(_hashKnockbackLevel, 0);
        animator.SetBool(_hashKnockbackDone, false);
        _knockbackEndFired = false;
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_knockbackEndFired) return;
        if (animator.speed == 0f) return;

        if (stateInfo.normalizedTime >= 1.0f)
        {
            _knockbackEndFired = true;
            animator.SetBool(_hashKnockbackDone, true);
            if (animator.TryGetComponent(out IEnemyAnimationController c))
                c.AnimEvent_KnockbackEnd();
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // 次回入場時の残留を防ぐためリセットする
        // KnockbackEnd は OnStateUpdate で自然完了時にのみ発火済みのためここでは発火しない
        animator.SetBool(_hashKnockbackDone, false);
    }

    // アニメーション自然完了で発火済みかどうかのフラグ
    // 各ステートが独立したSMBインスタンスを持つためフィールドで管理して問題ない
    private bool _knockbackEndFired;

    private static readonly int _hashIsKnockback = Animator.StringToHash("IsKnockback");
    private static readonly int _hashKnockbackLevel = Animator.StringToHash("KnockbackLevel");
    private static readonly int _hashKnockbackDone = Animator.StringToHash("KnockbackDone");
}
