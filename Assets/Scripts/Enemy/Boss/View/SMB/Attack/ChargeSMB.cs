using BossEnemy.SMB;
using UnityEngine;

namespace BossEnemy.SMB
{
    public class ChargeSMB : AttackSMBBase
    {
        protected override string AttackStartVoiceCueName => SoundCueNames.Boss.RushAttackVoice;

        protected override string AttackCueName => SoundCueNames.Boss.RushAttack;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnStateEnter(animator, stateInfo, layerIndex);
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnStateUpdate(animator, stateInfo, layerIndex);
            
            if(_elapsedSeconds > _startMoveTime && _elapsedSeconds < _endMoveTime)
            {
                if (_goalPos == Vector3.zero && _goalTime == 0)
                {
                    SetMoveGoal();
                    _animationEventReceiver.AnimEvent_ColliderIsTriggerIsEnabled(true);
                }

                _animationEventReceiver.AnimEvent_Move(_goalPos, _goalTime);
            }
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            _animationEventReceiver.AnimEvent_ColliderIsTriggerIsEnabled(false);
            base.OnStateExit(animator, stateInfo, layerIndex);
            _goalPos = Vector3.zero;
            _goalTime = 0;
        }

        [Header("移動開始時間")]
        [SerializeField] private float _startMoveTime = 0.8f;

        [Header("移動終了時間")]
        [SerializeField] private float _endMoveTime = 1f;

        // 移動地点とかける時間
        private Vector3 _goalPos = Vector3.zero;
        private float _goalTime = 0;

        private void SetMoveGoal()
        {
            Vector3 movementVector = _bossEnemyTransform.forward * _attackData.AttackStartDistance;
            _goalPos = movementVector + _bossEnemyTransform.position;

            _goalTime = _endMoveTime - _elapsedSeconds;
        }
    }
}
