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
        _animationVersion = controller != null ? controller.CombatAnimationVersion : -1;
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!IsCurrentAnimation) { return; }
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
            _isDodgeEnded = true;
            _playerAnimationController.AnimEvent_DodgeEnd();
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (IsCurrentAnimation && !_isDodgeEnded)
        {
            _isDodgeEnded = true;
            _playerAnimationController.AnimEvent_DodgeEnd();
        }
    }

    [Header("Timings (seconds)")]
    [SerializeField, Tooltip("無敵の開始時間")] private float _invincibleStartTime = 0.05f;
    [SerializeField, Tooltip("回避の終了時間(アニメーションより長い時間の場合はステートを抜ける際に自動的に終了する)")] private float _dodgeEndTime = 999f;

    private PlayerAnimationController _playerAnimationController;
    private int _animationVersion;
    private bool IsCurrentAnimation => _playerAnimationController != null
        && _animationVersion == _playerAnimationController.CombatAnimationVersion;
    private bool _invincibilityStarted;
    private bool _isDodgeEnded;
    private float _stateLength;
}
