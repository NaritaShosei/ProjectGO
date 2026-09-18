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

            // 突進は地面に沿って行う。Animatorのルートモーションなどで
            // transform.forward にY成分が混ざっても、目標座標へ持ち込まない。
            Vector3 startPosition = _bossCharacterTransform.position;
            if(TryGetHorizontalForward(_bossCharacterTransform.forward, out Vector3 result))
            {
                _canCharge = true;
                _currentHorizontalForward = result;
                _goalPos = startPosition + result * _moveDistance;
                _goalPos.y = startPosition.y;
            }
            else
            {
                _canCharge = false;
                _currentHorizontalForward = Vector3.zero;
                _goalPos = startPosition;
            }

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
            _canCharge = false;
        }

        [Header("移動距離")]
        [SerializeField] private float _moveDistance = 7.0f;

        [Header("移動開始から終了までの時間")]
        [SerializeField] private float _goalTime = 1f;

        [Header("攻撃の範囲エフェクトの生成位置の高さ")]
        [SerializeField] private float _attackAreaCircleGeneratePosY = 0.2f;

        // 移動地点とかける時間
        private Vector3 _goalPos = Vector3.zero;
        private bool _isMoving = false;
        private Vector3 _currentHorizontalForward;
        private bool _canCharge = false;

        protected async override UniTask PlayAttack(CancellationToken cancellationToken)
        {
            // 移動距離の半分の距離を攻撃エリアの中心にする
            float hitAreaSpawnCenterDistance = _moveDistance / 2;

            // 表示範囲も実際の突進と同じく水平面上に作成する。
            Vector3 spawnPosition = _bossCharacterTransform.position
                + _currentHorizontalForward * hitAreaSpawnCenterDistance;
            spawnPosition.y = _attackAreaCircleGeneratePosY;

            // 実判定は移動中のボスを中心とした円形範囲の連続判定。
            // その移動軌跡を、幅=円の直径・長さ=移動距離の矩形として表示する。
            float displayLength = _attackData.AttackHitAreaRadius * 2f;

            // 移動直線状に攻撃範囲を表示する
            HitAreaView hitArea = null;
            if (_canCharge)
            {
                // 移動直線状に攻撃範囲を表示する
                hitArea = _attackHitAreaSpawner.SpawnSquare(
                    spawnPosition,
                    displayLength,
                    _moveDistance,
                    _currentHorizontalForward);

                _visibleHitAreaList.Add(hitArea);
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

            if (hitArea != null)
            {
                // 移動終了と同時に攻撃範囲を隠す
                hitArea.InVisible();
                _visibleHitAreaList.Remove(hitArea);
            }

            // 移動,攻撃開始フラグをFalseに
            _isMoving = false;
            _isAttackHitCheck = false;
            _wasHitAttack = false;
        }

        protected override void StartAttackHitCheck(Vector3 attackCenterPos)
        {
            _attackCenterPos = attackCenterPos;
        }

        protected bool TryGetHorizontalForward(Vector3 forward, out Vector3 result)
        {
            result = Vector3.ProjectOnPlane(forward, Vector3.up);
            if (result.sqrMagnitude < 0.0001f)
                return false;

            result.Normalize();
            return true;
        }
    }
}
