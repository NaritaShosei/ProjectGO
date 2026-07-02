using UnityEngine;

public class EnemyFootstepSMB : StateMachineBehaviour
{ 
    public override void OnStateEnter(
       Animator animator,
       AnimatorStateInfo stateInfo,
       int layerIndex)
    {
        _leftFired = false;
        _rightFired = false;
        // 現在のループ数の記録
        _loopCount = Mathf.FloorToInt(stateInfo.normalizedTime);
    }

    public override void OnStateUpdate(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        if (animator.speed == 0f) return;

        int currentLoop = Mathf.FloorToInt(stateInfo.normalizedTime);

        if (currentLoop != _loopCount)
        {
            _loopCount = currentLoop;

            _leftFired = false;
            _rightFired = false;
        }

        float normalizedTime = stateInfo.normalizedTime % 1f;

        // 左足タイミング
        if (!_leftFired && normalizedTime >= _leftFootTime)
        {
            _leftFired = true;

            if (animator.TryGetComponent(out IEnemyAnimationController controller))
            {
                controller.AnimEvent_Footstep();
            }
        }

        // 右足タイミング
        if (!_rightFired &&
            normalizedTime >= _rightFootTime)
        {
            _rightFired = true;

            if (animator.TryGetComponent(out IEnemyAnimationController controller))
            {
                controller.AnimEvent_Footstep();
            }
        }
    }

    [SerializeField] private float _leftFootTime = 0.1f;

    [SerializeField] private float _rightFootTime = 0.557f;

    private bool _leftFired;
    private bool _rightFired;
    private int _loopCount;
}
