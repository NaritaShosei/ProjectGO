using BossEnemy.Effect;
using BossEnemy.Logic;
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
            // 移動フラグの初期化
            _isMoving = false;

            // 移動先を指定
            _goalPos =
                _bossCharacterTransform.position +
                (_bossCharacterTransform.forward * _moveDistance);

            base.OnStateEnter(animator, stateInfo, layerIndex);
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnStateUpdate(animator, stateInfo, layerIndex);

            // 移動フラグがTrueになっていれば移動を行う
            if (_isMoving)
            {
                // 残りの移動時間を算出
                var remainingMovementTime = _goalTime + _attackStartTime - _elapsedTime;

                _animationEventReceiver.AnimEvent_MoveCharacter(_goalPos, remainingMovementTime);

                StartAttackHitCheck(_bossCharacterTransform.position);
            }
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnStateExit(animator, stateInfo, layerIndex);

            _goalPos = Vector3.zero;
        }

        [Header("移動距離")]
        [SerializeField] private float _moveDistance = 7.0f;

        [Header("移動開始から終了までの時間")]
        [SerializeField] private float _goalTime = 1f;

        // 移動地点とかける時間
        private Vector3 _goalPos = Vector3.zero;
        private bool _isMoving = false;

        protected async override UniTask PlayAttack(CancellationToken cancellationToken)
        {
            // 移動距離の半分の距離を攻撃エリアの中心にする
            float hitAreaSpawnCenterDistance = _moveDistance / 2;

            // transform.position（自身の現在地） + transform.forward（正面方向の単位ベクトル） * 距離
            Vector3 spawnPosition =
                _bossCharacterTransform.position +
                (_bossCharacterTransform.forward * hitAreaSpawnCenterDistance);

            // 実判定は移動中のボスを中心とした円形範囲の連続判定。
            // その移動軌跡を、幅=円の直径・長さ=移動距離の矩形として表示する。
            float displayWidth = _attackData.AttackHitAreaRadius * 2f;
            float displayDuration = _attackStartTime + _goalTime;

            // 移動直線状に攻撃範囲を表示する
            HitAreaView hitArea = _attackHitAreaSpawner.Spawn(
                AttackHitAreaType.Square,
                spawnPosition,
                _attackData.AttackHitAreaRadius,
                _bossCharacterTransform.forward);

            // 攻撃範囲の大きさを設定
            if (hitArea is SquareHitAreaView squareHitArea)
            {
                // SquareHitAreaView は width をローカル X（ボスの横幅）、
                // length をローカル Z（transform.forward の進行方向）として扱う。
                // 進行距離を width に渡すと、範囲表示がボスの横方向に伸びてしまう。
                squareHitArea.SetSize(displayWidth, _moveDistance);
            }

            // 移動開始までの待機
            await UniTask.WaitUntil(() =>
                _elapsedTime >= _attackStartTime,
                cancellationToken: cancellationToken);

            // 移動,攻撃開始フラグを立てる
            _isMoving = true;
            _isAttackHitCheck = true;

            // 移動時間が終了するまで待機
            await UniTask.WaitUntil(() => 
                _elapsedTime >= _attackStartTime + _goalTime,
                cancellationToken: cancellationToken);

            // 移動終了と同時に攻撃範囲を
            hitArea.InVisible();

            // 移動,攻撃開始フラグをFalseに
            _isMoving = false;
            _isAttackHitCheck = false;
            _wasHitAttack = false;
        }

        protected override void StartAttackHitCheck(Vector3 attackCenterPos)
        {
            _attackCenterPos = attackCenterPos;
        }
    }
}
