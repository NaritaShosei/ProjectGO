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
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnStateExit(animator, stateInfo, layerIndex);
        }

        [Header("ボスから正面の攻撃位置までの距離")]
        [SerializeField] private float _attackPointDistance;

        protected async override UniTask PlayAttack(CancellationToken cancellationToken)
        {
            // 攻撃位置を取得
            var attackCenterPos = _bossCharacterTransform.position + _bossCharacterTransform.forward * _attackPointDistance;
            attackCenterPos.y = 0f; // 高さは基本使わないが念のため0にしておく

            // 攻撃音の再生
            PlayBossSE(SoundCueNames.Boss.HandSweep);

            // カメラの振動効果
            _cameraManager.ExecutionCameraShake(_cameraShakeData).Forget();

            // 攻撃の当たり判定を開始
            StartAttackHitCheck(attackCenterPos);

            // 攻撃の当たり判定が終了するまで待機
            await UniTask.WaitUntil(() =>
                !_isAttackHitCheck,
                cancellationToken: cancellationToken);
        }
    }
}
