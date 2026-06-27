using BossEnemy.SMB;
using UnityEngine;

namespace BossEnemy.SMB
{
    public class ChargeSMB : AttackSMBBase
    {
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnStateEnter(animator, stateInfo, layerIndex);
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnStateUpdate(animator, stateInfo, layerIndex);
            
            if(_elapsedSeconds > _startMoveTime && _elapsedSeconds < _endMoveTime)
            {
                Vector3 movementVector = _bossEnemyTransform.forward * _attackData.AttackHitDistance;
                Vector3 goalPos = movementVector + _bossEnemyTransform.position;

                float goalTime = _endMoveTime - _elapsedSeconds;

                _animationEventReceiver.AnimEvent_Move(goalPos, goalTime);
            }
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnStateExit(animator, stateInfo, layerIndex);
        }

        [Header("移動開始時間")]
        [SerializeField] private float _startMoveTime = 0.8f;

        [Header("移動終了時間")]
        [SerializeField] private float _endMoveTime = 1.6f;
    }
}
