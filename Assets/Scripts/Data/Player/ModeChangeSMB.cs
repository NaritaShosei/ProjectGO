using UnityEngine;

/// <summary>
/// モードチェンジアニメーション中にスロー演出を行うSMB
/// normalizedTimeでスロー開始・終了タイミングを指定する
/// </summary>
public class ModeChangeSMB : StateMachineBehaviour
{
    public override void OnStateEnter(
        Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _slowApplied = false;

        _hitStopManager = ServiceLocator.Get<HitStopManager>();

        if (animator.TryGetComponent(out IModeChangeAnimationController modeChangeAnimController))
        {
            _modeChangeAnimController = modeChangeAnimController;
        }
    }

    public override void OnStateUpdate(
        Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        float currentTime = stateInfo.normalizedTime * stateInfo.length;

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
    private IModeChangeAnimationController _modeChangeAnimController;
    private bool _slowApplied;
    private bool _modeChangeEnded;
}
