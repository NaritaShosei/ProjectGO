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
        float t = stateInfo.normalizedTime;

        if (_controller == null) { return; }

        if (!_attackExecuted && t >= _attackExecuteTime)
        {
            _attackExecuted = true;
            _controller.AnimEvent_AttackExecute();
        }

        if (!_comboStarted && t >= _comboWindowStartTime)
        {
            _comboStarted = true;
            _controller.AnimEvent_ComboWindowStart();
        }

        if (!_comboEnded && t >= _comboWindowEndTime)
        {
            _comboEnded = true;
            _controller.AnimEvent_ComboWindowEnd();
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _controller.AnimEvent_AttackComplete();
    }



    [Header("Timings (0〜1)")]
    [SerializeField, Range(0, 1)] private float _attackExecuteTime = 0.4f;
    [SerializeField, Range(0, 1)] private float _comboWindowStartTime = 0.6f;
    [SerializeField, Range(0, 1)] private float _comboWindowEndTime = 0.8f;

    private PlayerAnimationController _controller;

    private bool _attackExecuted;
    private bool _comboStarted;
    private bool _comboEnded;
}
