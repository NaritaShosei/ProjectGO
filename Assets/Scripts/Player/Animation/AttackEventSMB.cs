using UnityEngine;

public class AttackEventSMB : StateMachineBehaviour
{
    public override void OnStateEnter(
       Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _attackExecuteTimes ??= new float[0];
        _attackExecuted = new bool[_attackExecuteTimes.Length];
        _comboStarted = false;
        _comboEnded = false;
        _modeChangeReady = false;
        _attackCompleted = false;
        _stateLength = stateInfo.length;

        animator.TryGetComponent(out _controller);
        _animationVersion = _controller != null ? _controller.CombatAnimationVersion : -1;
    }

    public override void OnStateUpdate(
    Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (animator.speed == 0f) { return; }
        if (!IsCurrentAnimation) { return; }

        float currentTime = stateInfo.normalizedTime * _stateLength;

        for (int i = 0; i < _attackExecuteTimes.Length; i++)
        {
            if (_attackExecuted[i] || currentTime < _attackExecuteTimes[i]) continue;

            _attackExecuted[i] = true;
            _controller.AnimEvent_AttackExecute(i, _attackExecuteTimes.Length);
            if (!IsCurrentAnimation) return;
        }

        if (!_comboStarted && currentTime >= _comboWindowStartTime)
        {
            _comboStarted = true;
            _controller.AnimEvent_ComboWindowStart();
            if (!IsCurrentAnimation) return;
        }

        if (!_comboEnded && currentTime >= _comboWindowEndTime)
        {
            _comboEnded = true;
            _controller.AnimEvent_ComboWindowEnd();
            if (!IsCurrentAnimation) return;
            _controller.AnimEvent_ComboTransition();
            if (!IsCurrentAnimation) return;
        }

        if (!_modeChangeReady && currentTime >= _modeChangeTime)
        {
            _modeChangeReady = true;
            _controller.AnimEvent_ModeChangeReady();
            if (!IsCurrentAnimation) return;
        }

        if (!_attackCompleted && currentTime >= _attackCompleteTime)
        {
            _attackCompleted = true;
            _controller.AnimEvent_AttackComplete();
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!IsCurrentAnimation) { return; }

        if (!_attackCompleted)
        {
            _attackCompleted = true;
            _controller.AnimEvent_AttackComplete();
        }
    }

    [Header("Timings (seconds)")]
    [SerializeField] private float[] _attackExecuteTimes;
    [SerializeField] private float _comboWindowStartTime = 0.35f;
    [SerializeField] private float _comboWindowEndTime = 0.55f;
    [Tooltip("攻撃中にモードチェンジ入力を受け付け始める時刻（秒）")]
    [SerializeField] private float _modeChangeTime = 0.7f;
    [SerializeField] private float _attackCompleteTime = 999f;

    private PlayerAnimationController _controller;
    private int _animationVersion;
    private bool IsCurrentAnimation => _controller != null
        && _animationVersion == _controller.CombatAnimationVersion;

    private bool[] _attackExecuted;
    private bool _comboStarted;
    private bool _comboEnded;
    private bool _modeChangeReady;
    private bool _attackCompleted;
    private float _stateLength;
}
