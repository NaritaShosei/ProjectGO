using BossEnemy.Effect;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;

namespace BossEnemy.SMB
{
    public class StompingLeftSMB : AttackSMB
    {
        private const string START_MAGIC_EFFECT = "StartMagic";

        private const string PLAY_EFFECT_NAME = "BigRockUpLift";

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

        [Header("攻撃のEffect再生から当たり判定を開始するまでの時間")]
        [SerializeField] private float _attackHitTimingDelayTime;

        [Header("攻撃の範囲エフェクトの生成位置の高さ")]
        [SerializeField] private float _attackAreaCircleGeneratePosY = 0.2f;

        [Header("魔法攻撃の発動Effect発生位置")]
        [SerializeField] private Vector3 _magicStartEffectPlayOffset;

        [Header("ボスから正面の攻撃位置までの距離")]
        [SerializeField] private float _attackPointDistance;

        protected async override UniTask PlayAttack(CancellationToken cancellationToken)
        {
            // 攻撃位置を取得
            var attackCenterPos = _bossCharacterTransform.position + _bossCharacterTransform.forward * _attackPointDistance;
            attackCenterPos.y = _attackAreaCircleGeneratePosY;

            // 攻撃範囲を表示
            HitAreaView hitArea = _attackHitAreaSpawner.Spawn(
                AttackHitAreaType.Circle,
                attackCenterPos,
                _attackData.AttackHitAreaRadius);

            _visibleHitAreaList.Add(hitArea);

            // 攻撃のEffectは発生するまでの時間
            var playEffectStartTime = _elapsedTime + _attackAreaDespawnAndDisplayAttackEffectTime;

            // 攻撃範囲が生成されてから攻撃が行われるまでの時間待機
            await UniTask.WaitUntil(() =>
                _elapsedTime >= playEffectStartTime,
                cancellationToken: cancellationToken);

            // 魔法攻撃の発動Effect発生位置を取得
            var magicStartEffectPlayPos = _bossCharacterTransform.TransformPoint(_magicStartEffectPlayOffset);

            // 魔法攻撃の発動Effectを再生
            _effectManager.PlayEffect(START_MAGIC_EFFECT, magicStartEffectPlayPos);

            // 攻撃範囲を見えなくする
            hitArea.InVisible();
            _visibleHitAreaList.Remove(hitArea);

            // 攻撃音の再生
            PlayBossSE(SoundCueNames.Boss.RockEruption);

            // 攻撃Effectを再生
            _effectManager.PlayEffect(PLAY_EFFECT_NAME, attackCenterPos);

            // カメラの振動効果
            _cameraManager.ExecutionCameraShake(_cameraShakeData).Forget();

            // 攻撃の当たり判定が開始されるまでの時間
            var attackHitCheckStartTime = _elapsedTime + _attackHitTimingDelayTime;

            // 攻撃のEffect再生から当たり判定を開始するまでの遅延
            await UniTask.WaitUntil(() =>
                _elapsedTime >= attackHitCheckStartTime,
                cancellationToken: cancellationToken);

            // 攻撃の当たり判定を開始
            StartAttackHitCheck(attackCenterPos);

            // 攻撃の当たり判定が終了するまで待機
            await UniTask.WaitUntil(() =>
                !_isAttackHitCheck,
                cancellationToken: cancellationToken);
        }
    }
}
