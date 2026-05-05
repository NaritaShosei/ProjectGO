using UnityEngine;

/// <summary>
/// GetUp ステートにアタッチするSMB。
/// OnStateEnter : GetUpDone をリセットする。
/// OnStateUpdate: アニメーション自然完了（normalizedTime >= 1）を検出して GetUpEnd を発火する。
///                HasExitTime の代替。ノックバック割り込み時は GetUpEnd を発火しない。
/// OnStateExit  : GetUpDone をリセットする。割り込み退場時は GetUpEnd を発火しない。
/// </summary>
public class EnemyGetUpSMB : StateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetBool(_hashGetUpDone, false);
        _getUpEndFired = false;
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_getUpEndFired) return;
        if (animator.speed == 0f) return;

        if (stateInfo.normalizedTime >= 1.0f)
        {
            _getUpEndFired = true;
            animator.SetBool(_hashGetUpDone, true);
            if (animator.TryGetComponent(out IEnemyAnimationController c))
                c.AnimEvent_GetUpEnd();
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // 次回入場時の残留を防ぐためリセットする
        // GetUpEnd は OnStateUpdate で自然完了時にのみ発火済みのためここでは発火しない
        animator.SetBool(_hashGetUpDone, false);
    }

    private bool _getUpEndFired;

    private static readonly int _hashGetUpDone = Animator.StringToHash("GetUpDone");
}
