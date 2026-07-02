//BossEnemy関連
using BossEnemy.Model.System;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace BossEnemy.View.SMB
{
    public class MeteorSMB : AttackSMBBase
    {
        protected override string AttackStartVoiceCueName => SoundCueNames.Boss.MeteorVoice;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
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

            // 攻撃開始
            if (_attackHitFired)
            {
                MeteorSequence(_cts.Token).Forget();
                _attackHitFired = false;
            }
        }

        [Header("攻撃の間隔を開ける秒数")]
        [SerializeField] private float _consecutiveAttackInterval;

        [Header("攻撃の回数")]
        [SerializeField] private int _maxAttackCount = 20;

        [Header("攻撃の範囲エフェクトの生成の高さ")]
        [SerializeField] private float _attackAreaEffectPosY = 0.2f;

        [Header("攻撃着弾地点の高さ")]
        [SerializeField] private float _meteorEffectPosY = -1f;

        [Header("隕石発射から到達までの秒数")]
        [SerializeField] private float _attackHitTime = 1.05f;

        private CancellationTokenSource _cts = new();

        private EffectManager _effectManager;

        private void Awake()
        {
            _effectManager = FindFirstObjectByType<EffectManager>();
        }

        private async UniTask MeteorSequence(CancellationToken cancellationToken)
        {
            List<Vector3> attackPosList = new();

            for (int count = 0; count < _maxAttackCount; count++)
            {
                float attackAreaX = UnityEngine.Random.Range(_bossEnemyTransform.position.x - _attackData.AttackStartDistance, _bossEnemyTransform.position.x + _attackData.AttackStartDistance);
                float attackAreaZ = UnityEngine.Random.Range(_bossEnemyTransform.position.z - _attackData.AttackStartDistance, _bossEnemyTransform.position.z + _attackData.AttackStartDistance);
                Vector3 attackCenter = new Vector3(attackAreaX, _attackAreaEffectPosY, attackAreaZ);
                float despawnTime = _attackAreaDespawnTime + _attackHitTime + (count * (_attackHitTime + _consecutiveAttackInterval));

                _attackHitAreaSpawner.Spawn(HitAreaType.Circle, attackCenter, _attackData.AttackRange, despawnTime);
                attackCenter.y = _meteorEffectPosY;
                attackPosList.Add(attackCenter);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(_attackAreaDespawnTime), cancellationToken: cancellationToken);

            foreach (Vector3 attackPos in attackPosList)
            {
                _effectManager.PlayEffect(_attackData.AnimParamName, attackPos);

                await UniTask.Delay(TimeSpan.FromSeconds(_attackHitTime), cancellationToken: cancellationToken);

                PlayBossSE(SoundCueNames.Boss.RockEruption);
                _cameraManager.ExecutionCameraShake(_cameraShakeData).Forget();
                if (AttackHitChecker.TryHitAttack(HitAreaType.Circle, attackPos, _target, _attackData.AttackRange))
                {
                    _animationEventReceiver.AnimEvent_AttackHit();
                    _attackHitFired = false;
                }

                await UniTask.Delay(TimeSpan.FromSeconds(_consecutiveAttackInterval), cancellationToken: cancellationToken);
            }
        }
    }
}
