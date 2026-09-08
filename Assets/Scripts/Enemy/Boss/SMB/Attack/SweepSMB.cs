using BossEnemy.Effect;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

namespace BossEnemy.SMB
{
    public class DownSweepSMB : AttackSMB
    {
        protected override string AttackStartVoiceCueName => SoundCueNames.Boss.ChargePunchVoice;

        protected override string AttackCueName => SoundCueNames.Boss.ChargePunch;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnStateEnter(animator, stateInfo, layerIndex);
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnStateUpdate(animator, stateInfo, layerIndex);

            if (_isAttackHitCheck && !_wasHitAttack)
            {
                _animationEventReceiver.AnimEvent_AttackHitCheck
                    (AttackHitAreaType.Circle, _attackAreaCenter);
            }
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnStateExit(animator, stateInfo, layerIndex);
        }

        [Header("攻撃ダメージ判定開始時間")]
        [SerializeField] private float _attackDistance;

        [Header("攻撃ダメージ判定開始時間")]
        [SerializeField] private float _startAttackHitTiming = 1f;

        [Header("攻撃ダメージ判定開始から終了までの時間")]
        [SerializeField] private float _endAttackHitTiming = 1f;

        private bool _isAttackHitCheck = false;

        protected async override UniTask PlayAttack(CancellationToken cancellationToken)
        {
            // transform.position（自身の現在地） + transform.forward（正面方向の単位ベクトル） * 距離
            Vector3 spawnPosition =
                _bossCharacterTransform.position +
                (_bossCharacterTransform.forward * _attackDistance);
            _attackAreaCenter = spawnPosition;

            float displayWidth = _attackData.AttackHitAreaRadius * 2f;
            float displayDuration = _startAttackHitTiming + _endAttackHitTiming;

            _attackHitAreaSpawner.Spawn(
                AttackHitAreaType.Circle,
                spawnPosition,
                _attackData.AttackHitAreaRadius,
                displayDuration);

            await UniTask.Delay(TimeSpan.FromSeconds(_startAttackHitTiming));

            _isAttackHitCheck = true;

            await UniTask.Delay(TimeSpan.FromSeconds(_endAttackHitTiming));

            _isAttackHitCheck = false;
        }
    }
}
