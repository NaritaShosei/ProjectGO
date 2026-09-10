using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

using BossEnemy.Enum;
using BossEnemy.Attack;

namespace BossEnemy.SMB
{
    public class RockUpliftSMB : AttackSMB
    {
        private const string ROCK_UP_LIFT_EFFECT_NAME = "RockUpLift";

        protected override string AttackStartVoiceCueName => SoundCueNames.Boss.RockEruptionVoice;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnStateEnter(animator, stateInfo, layerIndex);
            _cts = new();
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnStateUpdate(animator, stateInfo, layerIndex);
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnStateExit(animator, stateInfo, layerIndex);
        }

        [Header("攻撃ダメージ判定開始時間")]
        [SerializeField] private float _startAttackTime = 1f;


        [Header("攻撃の間隔を開けるフレーム数")]
        [SerializeField] private int _attackIntervalFlame;

        [Header("攻撃の範囲エフェクトの生成の高さ")]
        [SerializeField] private float _attackAreaCircleGeneratePosY = 0.2f;

        [Header("攻撃の回数")]
        [SerializeField] private int _maxAttackCount = 4;

        private CancellationTokenSource _cts;
        Vector3 _attackPos = Vector3.zero;

        private bool _isAttackHitCheck = false;

        private void Awake()
        {
            _effectManager = FindFirstObjectByType<EffectManager>();
        }

        protected async override UniTask PlayAttack(CancellationToken cancellationToken)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(_startAttackTime), cancellationToken: cancellationToken);

            for (int count = 0; count < _maxAttackCount; count++)
            {
                _isAttackHitCheck = true;
                _attackPos = _attackTarget.GetTargetCenter().position;
                _attackPos.y = _attackAreaCircleGeneratePosY;
                _attackHitAreaSpawner.Spawn(AttackHitAreaType.Circle, _attackPos, _attackData.AttackHitAreaRadius, _attackAreaDespawnTime);

                await UniTask.Delay(TimeSpan.FromSeconds(_attackAreaDespawnTime), cancellationToken: cancellationToken);

                PlayBossSE(SoundCueNames.Boss.RockEruption);
                _effectManager.PlayEffect(ROCK_UP_LIFT_EFFECT_NAME, _attackPos);

                _cameraManager.ExecutionCameraShake(_cameraShakeData).Forget();

                int waitPlayEffect = 40;
                await UniTask.Delay(waitPlayEffect);

                _animationEventReceiver.AnimEvent_AttackHitCheck
                    (AttackHitAreaType.Circle, _attackPos);

                await UniTask.Delay(_attackIntervalFlame);
            }
        }

        private void OnDestroy()
        {
            // 破棄時に非同期処理をキャンセル
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
