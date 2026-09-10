using BossEnemy.Effect;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;


namespace BossEnemy.SMB
{
    public class ChargeSMB : AttackSMB
    {
        protected override string AttackStartVoiceCueName => SoundCueNames.Boss.RushAttackVoice;

        protected override string AttackCueName => SoundCueNames.Boss.RushAttack;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            _goalPos =
                _bossCharacterTransform.position +
                (_bossCharacterTransform.forward * _moveDistance);

            base.OnStateEnter(animator, stateInfo, layerIndex);
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnStateUpdate(animator, stateInfo, layerIndex);

            // 攻撃の当たり判定フラグがTrueでまだ攻撃が当たっていなければ攻撃の当たり判定を行う
            if (_isAttackHitCheck && !_wasHitAttack)
            {
                _animationEventReceiver.AnimEvent_AttackHitCheck
                    (AttackHitAreaType.Circle, _bossCharacterTransform.position);
            }

            // 移動フラグがTrueになっていれば移動を行う
            if (_isMoving)
            {
                _goalArrivalTime -= Time.deltaTime * _timeScale;
                _animationEventReceiver.AnimEvent_MoveCharacter(_goalPos, _goalArrivalTime);

                if( _goalArrivalTime <= 0) _isMoving = false;
            }
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnStateExit(animator, stateInfo, layerIndex);

            _goalPos = Vector3.zero;
            _goalArrivalTime = 0f;

            if(_isAttackHitCheck) 
                _isAttackHitCheck = false;
        }

        [Header("移動距離")]
        [SerializeField] private float _moveDistance = 7.0f;

        [Header("移動開始時間")]
        [SerializeField] private float _startMoveTime = 1f;

        [Header("移動開始から終了までの時間")]
        [SerializeField] private float _endMoveTime = 1f;

        // 移動地点とかける時間
        private Vector3 _goalPos = Vector3.zero;
        private float _goalArrivalTime = 0f;
        private bool _isAttackHitCheck = false;
        private bool _isMoving = false;

        protected async override UniTask PlayAttack(CancellationToken cancellationToken)
        {
            try
            {
                float spawnDistance = _moveDistance / 2;

                // transform.position（自身の現在地） + transform.forward（正面方向の単位ベクトル） * 距離
                Vector3 spawnPosition =
                    _bossCharacterTransform.position +
                    (_bossCharacterTransform.forward * spawnDistance);

                // 実判定は移動中のボスを中心とした円形範囲の連続判定。
                // その移動軌跡を、幅=円の直径・長さ=移動距離の矩形として表示する。
                float displayWidth = _attackData.AttackHitAreaRadius * 2f;
                float displayDuration = _startMoveTime + _endMoveTime;


                HitAreaView hitArea = _attackHitAreaSpawner.Spawn(
                    AttackHitAreaType.Square,
                    spawnPosition,
                    _attackData.AttackHitAreaRadius,
                    displayDuration,
                    _bossCharacterTransform.forward);

                if (hitArea is SquareHitAreaView squareHitArea)
                {
                    squareHitArea.SetSize(displayWidth, _moveDistance);
                }

                await UniTask.Delay(TimeSpan.FromSeconds(_startMoveTime), cancellationToken: cancellationToken);

                _goalArrivalTime = _endMoveTime;
                _isAttackHitCheck = true;
                _isMoving = true;

                await UniTask.WaitUntil(() => !_isMoving, cancellationToken: cancellationToken);

                _isAttackHitCheck = false;
                _goalArrivalTime = 0;
            }
            catch (OperationCanceledException)
            {

            }
        }

        protected override void ChangeTimeScale(float timeScale)
        {
            _timeScale = timeScale;
        }
    }
}
