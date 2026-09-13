using UnityEngine;

public class ModeChangeSMB : StateMachineBehaviour
{
    public override void OnStateEnter(
        Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _slowApplied = false;
        _modeChangeEnded = false;
        _modeChangeAnimController = null;
        _stateLength = stateInfo.length;

        ServiceLocator.TryGet(out _hitStopManager);

        if (animator.TryGetComponent(out PlayerAnimationController modeChangeAnimController))
        {
            _modeChangeAnimController = modeChangeAnimController;
            _animationVersion = modeChangeAnimController.CombatAnimationVersion;
        }
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_modeChangeAnimController == null
            || _animationVersion != _modeChangeAnimController.CombatAnimationVersion) return;
        float currentTime = stateInfo.normalizedTime * _stateLength;

        if (!_slowApplied && currentTime >= _slowStartTime)
        {
            _slowApplied = true;
            _hitStopManager?.TriggerDirect(_slowDuration, _targetGroup, _slowTimeScale);
        }

        if (!_modeChangeEnded && currentTime >= _modeChangeEndTime)
        {
            _modeChangeEnded = true;
            _modeChangeAnimController?.AnimEvent_ModeChangeComplete();
        }
    }

    [Header("スロー区間")]
    [SerializeField] private float _slowStartTime = 0.2f;

    [Header("スロー演出設定")]
    [SerializeField] private float _slowDuration = 0.5f;
    [SerializeField] private HitStopTargetGroup _targetGroup = HitStopTargetGroup.Player | HitStopTargetGroup.AllEnemies | HitStopTargetGroup.Effects;
    [SerializeField] private float _slowTimeScale = 0.1f;

    [Header("モードチェンジ終了")]
    [SerializeField] private float _modeChangeEndTime = 0.8f;

    private HitStopManager _hitStopManager;
    private PlayerAnimationController _modeChangeAnimController;
    private int _animationVersion;
    private bool _slowApplied;
    private bool _modeChangeEnded;
    private float _stateLength;
}
