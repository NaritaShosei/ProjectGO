using UnityEngine;

//BossEnemy関連
using BossEnemy.Model.System;

namespace BossEnemy.View.SMB
{
    public class MeteorSMB : AttackSMBBase
    {
        protected override string AttackStartVoiceCueName => SoundCueNames.Boss.MeteorVoice;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            _attackCount = 0;
            _playedImpactCount = 0;
            base.OnStateEnter(animator, stateInfo, layerIndex);
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnStateUpdate(animator, stateInfo, layerIndex);
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnStateExit(animator, stateInfo, layerIndex);
        }

        public override void AttackSequence(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            // 溜め
            if (!_isChargeCompleted && _attackData.AttackChargeTime < _elapsedSeconds)
            {
                _attackHitFired = true;
                _isChargeCompleted = true;
            }

            // 攻撃エリア出現
            if (!_isAttackAreaActive && _elapsedSeconds > _attackData.AttackAreaEffectStartTime + (_attackCount * _consecutiveAttackInterval))
            {
                if (_maxAttackCount <= _attackCount) return;

                Debug.Log("隕石" + _attackCount + "回目の攻撃");
                _attackPos = _hitCenterPos + (_bossEnemyTransform.forward * (_attackDistance * _attackCount));
                _attackHitAreaSpawner.Spawn(HitAreaType.Circle, _attackPos, _attackData.AttackRange, _attackAreaDespawnTime);
                _isAttackAreaActive = true;
                _attackCount++;
            }

            // 攻撃開始
            if (_attackHitFired && _elapsedSeconds <= _attackData.AttackChargeTime + _attackData.AttackDuration)
            {
                if (_playedImpactCount < _attackCount)
                {
                    PlayBossSE(SoundCueNames.Boss.MeteorImpact);
                    _playedImpactCount++;
                }

                _effectManager.PlayEffect(_attackData.AnimParamName, _attackPos);

                if (AttackHitChecker.TryHitAttack(HitAreaType.Circle, _attackPos, _target, _attackData.AttackRange))
                {
                    _animationEventReceiver.AnimEvent_AttackHit();
                    _attackHitFired = false;
                }

                if (_attackData.AttackChargeTime + _attackData.AttackDuration < _elapsedSeconds)
                {
                    _attackHitFired = false;
                }

                _attackHitFired = true;
                _isAttackAreaActive = false;
            }
        }

        [SerializeField] private float _consecutiveAttackInterval;

        [SerializeField] private float _attackDistance;

        private int _attackCount = 0;
        private int _playedImpactCount = 0;
        private const int _maxAttackCount = 4;
        Vector3 _attackPos = Vector3.zero;

        private EffectManager _effectManager;

        private void Awake()
        {
            _effectManager = FindFirstObjectByType<EffectManager>();
        }
    }
}
