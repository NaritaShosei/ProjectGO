using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

// BossEnemy関連
using BossEnemy.Model.System;

namespace BossEnemy.View.SMB
{
    public class RockUpliftSMB : AttackSMBBase
    {
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

            if(animator.speed !=1) animator.speed = 1;
            _attackHitFired = false;
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
                RockUplift(_cts.Token).Forget();
                _attackHitFired = false;
            }
        }

        [Header("攻撃の間隔を開けるフレーム数")]
        [SerializeField] private int _attackIntervalFlame;

        [Header("攻撃の範囲エフェクトの生成の高さ")]
        [SerializeField] private float _attackAreaCircleGeneratePosY = 0.2f;

        [Header("攻撃の回数")]
        [SerializeField] private int _maxAttackCount = 4;

        private CancellationTokenSource _cts;
        Vector3 _attackPos = Vector3.zero;

        private EffectManager _effectManager;

        private void Awake()
        {
            _effectManager = FindFirstObjectByType<EffectManager>();
        }

        private async UniTask RockUplift(CancellationToken cancellationToken)
        {
            for(int count = 0; count < _maxAttackCount; count++)
            {
                _attackHitFired = true;
                _attackPos = _target.GetTargetCenter().position;
                _attackPos.y = _attackAreaCircleGeneratePosY;
                _attackHitAreaSpawner.Spawn(HitAreaType.Circle, _attackPos, _attackData.AttackRange, _attackAreaDespawnTime);

                await UniTask.Delay(TimeSpan.FromSeconds(_attackAreaDespawnTime), cancellationToken: cancellationToken);

                PlayBossSE(SoundCueNames.Boss.RockEruption);
                _effectManager.PlayEffect(_attackData.AnimParamName, _attackPos);

                _cameraManager.ExecutionCameraShake(_cameraShakeData).Forget();
                if (AttackHitChecker.TryHitAttack(HitAreaType.Circle, _attackPos, _target, _attackData.AttackRange))
                {
                    _animationEventReceiver.AnimEvent_AttackHit();
                    _attackHitFired = false;
                }

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
