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
    }

    public override void OnStateUpdate(
    Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (animator.speed == 0f) { return; }
        if (_controller == null) { return; }

        float currentTime = stateInfo.normalizedTime * _stateLength;

        for (int i = 0; i < _attackExecuteTimes.Length; i++)
        {
            if (_attackExecuted[i] || currentTime < _attackExecuteTimes[i]) continue;

            _attackExecuted[i] = true;
            _controller.AnimEvent_AttackExecute(i, _attackExecuteTimes.Length);
        }

        if (!_comboStarted && currentTime >= _comboWindowStartTime)
        {
            _comboStarted = true;
            _controller.AnimEvent_ComboWindowStart();
        }

        if (!_comboEnded && currentTime >= _comboWindowEndTime)
        {
            _comboEnded = true;
            _controller.AnimEvent_ComboWindowEnd();
            _controller.AnimEvent_ComboTransition();
        }

        if (!_modeChangeReady && currentTime >= _modeChangeTime)
        {
            _modeChangeReady = true;
            _controller.AnimEvent_ModeChangeReady();
        }

        if (!_attackCompleted && currentTime >= _attackCompleteTime)
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

    private IAnimationController _controller;

    private bool[] _attackExecuted;
    private bool _comboStarted;
    private bool _comboEnded;
    private bool _modeChangeReady;
    private bool _attackCompleted;
    private float _stateLength;
}
