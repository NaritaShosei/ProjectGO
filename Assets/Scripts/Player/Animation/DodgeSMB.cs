using UnityEngine;

/// <summary>
/// Dodge ステートにアタッチする SMB。
/// ステート終了時に DodgeEnd を発火して PlayerAnimationController へ通知する。
/// </summary>
public class DodgeSMB : StateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _invincibilityStarted = false;

        if (animator.TryGetComponent(out PlayerAnimationController controller))
            _playerAnimationController = controller;

        var clipInfo = animator.GetCurrentAnimatorClipInfo(layerIndex);

        if (clipInfo.Length > 0)
            _frameRate = clipInfo[0].clip.frameRate;
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_playerAnimationController == null) { return; }

        if (_invincibilityStarted) { return; }

        int currentFrame = (int)(stateInfo.normalizedTime * stateInfo.length * _frameRate);

        // 無敵開始フレームに達したら無敵開始
        if (currentFrame >= _dodgeInvincibilityStartFrame)
        {
            _playerAnimationController.AnimEvent_DodgeInvincibilityStart();
            _invincibilityStarted = true;
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_playerAnimationController != null)
            _playerAnimationController.AnimEvent_DodgeEnd();
    }

    [Header("Dodge Timings (frame)")]
    [SerializeField] private int _dodgeInvincibilityStartFrame = 3;

    private PlayerAnimationController _playerAnimationController;
    private bool _invincibilityStarted;
    private float _frameRate;
}
