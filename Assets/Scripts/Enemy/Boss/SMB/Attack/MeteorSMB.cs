using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

using BossEnemy.Enum;
using BossEnemy.Attack;

namespace BossEnemy.SMB
{
    public class MeteorSMB : AttackSMB
    {
        private const string METEOR_EFFECT_NAME = "Meteor";

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

        [Header("攻撃の間隔を開ける秒数")]
        [SerializeField] private float _consecutiveAttackInterval;

        [Header("攻撃範囲の広さ正方形の1辺の半分")]
        [SerializeField] private float _maxAttackFieldHalfRange = 20;

        [Header("攻撃の回数")]
        [SerializeField] private int _maxAttackCount = 20;

        [Header("攻撃の範囲エフェクトの生成の高さ")]
        [SerializeField] private float _attackAreaEffectPosY = 0.2f;

        [Header("攻撃着弾地点の高さ")]
        [SerializeField] private float _meteorEffectPosY = -1f;

        [Header("隕石発射から到達までの秒数")]
        [SerializeField] private float _attackHitTime = 1.05f;

        private void Awake()
        {
            _effectManager = FindFirstObjectByType<EffectManager>();
        }

        protected async override UniTask PlayAttack(CancellationToken cancellationToken)
        {
            List<Vector3> attackPosList = new();

            for (int count = 0; count < _maxAttackCount; count++)
            {
                float attackAreaX = UnityEngine.Random.Range
                    (_attackTarget.GetTargetCenter().position.x - _maxAttackFieldHalfRange,
                    _attackTarget.GetTargetCenter().position.x + _maxAttackFieldHalfRange);

                float attackAreaZ = UnityEngine.Random.Range
                    (_attackTarget.GetTargetCenter().position.z - _maxAttackFieldHalfRange,
                    _attackTarget.GetTargetCenter().position.z + _maxAttackFieldHalfRange);

                Vector3 attackCenter = new Vector3(attackAreaX, _attackAreaEffectPosY, attackAreaZ);

                attackCenter.y = _meteorEffectPosY;
                attackPosList.Add(attackCenter);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(_attackAreaDespawnTime), cancellationToken: cancellationToken);
            int attackCount = 0;

            foreach (Vector3 attackPos in attackPosList)
            {
                float despawnTime =
                    _attackAreaDespawnTime +
                    _attackHitTime +
                    (attackCount * (_attackHitTime + _consecutiveAttackInterval));

                attackCount++;

                _attackHitAreaSpawner.Spawn
                    (AttackHitAreaType.Circle, attackPos, _attackData.AttackHitAreaRadius, despawnTime);

                _effectManager.PlayEffect(METEOR_EFFECT_NAME, attackPos);

                await UniTask.Delay(TimeSpan.FromSeconds(_attackHitTime), cancellationToken: cancellationToken);

                PlayBossSE(SoundCueNames.Boss.MeteorImpact);
                _cameraManager.ExecutionCameraShake(_cameraShakeData).Forget();

                _animationEventReceiver.AnimEvent_AttackHitCheck
                    (AttackHitAreaType.Circle, _attackAreaCenter);

                await UniTask.Delay(TimeSpan.FromSeconds(_consecutiveAttackInterval), cancellationToken: cancellationToken);
            }
        }
    }
}
