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

        if (animator.TryGetComponent(out IModeChangeAnimationController modeChangeAnimController))
        {
            _modeChangeAnimController = modeChangeAnimController;
        }
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        float currentTime = stateInfo.normalizedTime * _stateLength;

        if (!_slowApplied && currentTime >= _slowStartTime)
        {
            _slowApplied = true;
            _hitStopManager?.TriggerDirect(_slowDuration, _targetGroup, _slowTimeScale);
        }

        if (!_modeChangeEnded && currentTime >= _modeChangeEndTime)
        {
            _modeChangeEnded = true;
            // ここでPlayerModeを更新することでBlendTree切り替えをアニメーション完了後に行う
            animator.SetInteger(Animator.StringToHash("PlayerMode"), 1); // Thunder
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
    private float _stateLength;
}
