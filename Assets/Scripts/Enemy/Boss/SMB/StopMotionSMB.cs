using UnityEngine;

public class StopMotionSMB : StateMachineBehaviour
{
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if(!_isStop && stateInfo.length < _stopTime)
        {
            animator.speed = 0;
            _isStop = true;
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _isStop = false;
        animator.speed = 1;
    }

    private bool _isStop = false;
    [SerializeField] private float _stopTime = 1.0f;  
}
