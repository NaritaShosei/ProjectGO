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
        _stateLength = stateInfo.length;

        if (animator.TryGetComponent(out PlayerAnimationController controller))
            _playerAnimationController = controller;
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_playerAnimationController == null) { return; }
        float currentTime = stateInfo.normalizedTime * _stateLength;

        if (!_invincibilityStarted &&
            currentTime >= _invincibleStartTime)
        {
            _playerAnimationController.AnimEvent_DodgeInvincibilityStart();
            _invincibilityStarted = true;
        }

        if (!_isDodgeEnded &&
            currentTime >= _dodgeEndTime)
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

    [Header("Timings (seconds)")]
    [SerializeField, Tooltip("無敵の開始時間")] private float _invincibleStartTime = 0.05f;
    [SerializeField, Tooltip("回避の終了時間(アニメーションより長い時間の場合はステートを抜ける際に自動的に終了する)")] private float _dodgeEndTime = 999f;

    private PlayerAnimationController _playerAnimationController;
    private bool _invincibilityStarted;
    private bool _isDodgeEnded;
    private float _stateLength;
}
