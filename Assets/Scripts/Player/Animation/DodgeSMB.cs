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
        _isDodgeEnded = false;

        if (animator.TryGetComponent(out PlayerAnimationController controller))
            _playerAnimationController = controller;

        var clipInfo = animator.GetCurrentAnimatorClipInfo(layerIndex);

        if (clipInfo.Length > 0)
            _frameRate = clipInfo[0].clip.frameRate;
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_playerAnimationController == null) { return; }

        int currentFrame = (int)(stateInfo.normalizedTime * stateInfo.length * _frameRate);

        // 無敵開始フレームに達したら無敵開始
        if (currentFrame >= _dodgeInvincibilityStartFrame && !_invincibilityStarted)
        {
            _playerAnimationController.AnimEvent_DodgeInvincibilityStart();
            _invincibilityStarted = true;
        }

        if (currentFrame >= _dodgeEndFrame && !_isDodgeEnded)
        {
            _playerAnimationController.AnimEvent_DodgeEnd();
            _isDodgeEnded = true;
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_playerAnimationController != null && !_isDodgeEnded)
        {
            _playerAnimationController.AnimEvent_DodgeEnd();
            _isDodgeEnded = true;
        }
    }

    [Header("Dodge Timings (frame)")]
    [SerializeField, Tooltip("回避の無敵時間の開始フレーム")] private int _dodgeInvincibilityStartFrame = 3;
    [SerializeField, Tooltip("回避終了、移動可能になるフレーム")] private int _dodgeEndFrame = 9999;

    private PlayerAnimationController _playerAnimationController;
    private bool _invincibilityStarted;
    private bool _isDodgeEnded;
    private float _frameRate;
}
