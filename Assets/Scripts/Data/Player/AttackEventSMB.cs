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
        _attackCompleted = false;
    }

    public override void OnStateUpdate(
        Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        float t = stateInfo.normalizedTime;

        var controller = animator.GetComponent<PlayerAnimationController>();
        if (controller == null) { return; }

        if (!_attackExecuted && t >= _attackExecuteTime)
        {
            _attackExecuted = true;
            controller.AnimEvent_AttackExecute();
        }

        if (!_comboStarted && t >= _comboWindowStartTime)
        {
            _comboStarted = true;
            controller.AnimEvent_ComboWindowStart();
        }

        if (!_comboEnded && t >= _comboWindowEndTime)
        {
            _comboEnded = true;
            controller.AnimEvent_ComboWindowEnd();
        }

        if (!_attackCompleted && t >= _attackCompleteTime)
        {
            _attackCompleted = true;
            controller.AnimEvent_AttackComplete();
        }
    }

    [Header("Timings (0〜1)")]
    [SerializeField, Range(0, 1)] private float _attackExecuteTime = 0.4f;
    [SerializeField, Range(0, 1)] private float _comboWindowStartTime = 0.6f;
    [SerializeField, Range(0, 1)] private float _comboWindowEndTime = 0.8f;
    [SerializeField, Range(0, 1)] private float _attackCompleteTime = 0.8f;

    private bool _attackExecuted;
    private bool _comboStarted;
    private bool _comboEnded;
    private bool _attackCompleted;
}
