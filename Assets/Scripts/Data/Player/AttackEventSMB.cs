using UnityEngine;

public class AttackEventSMB : StateMachineBehaviour
{
    public override void OnStateEnter(
       Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // 毎回リセット
        _attackExecuted = false;
        _comboStarted = false;
        _comboEnded = false;

        animator.TryGetComponent(out _controller);
    }

    public override void OnStateUpdate(
    Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_controller == null) return;

        float currentTime = stateInfo.normalizedTime * stateInfo.length;

        if (!_attackExecuted && currentTime >= _attackExecuteTime)
        {
            _attackExecuted = true;
            _controller.AnimEvent_AttackExecute();
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
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_controller == null) return;
        _controller.AnimEvent_AttackComplete();
    }

    [Header("Timings (seconds)")]
    [SerializeField] private float _attackExecuteTime = 0.2f;
    [SerializeField] private float _comboWindowStartTime = 0.35f;
    [SerializeField] private float _comboWindowEndTime = 0.55f;

    private IAnimationController _controller;

    private bool _attackExecuted;
    private bool _comboStarted;
    private bool _comboEnded;
}
