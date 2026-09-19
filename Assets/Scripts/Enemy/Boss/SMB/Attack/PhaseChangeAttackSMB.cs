using BossEnemy.Attack;
using BossEnemy.Effect;
using BossEnemy.SMB;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace BossEnemy.Logic
{
    public class PhaseChangeAttackSMB : AttackSMB
    {
        private const string IMPACT_EFFECT = "Impact";

        private const string BIG_ROCK_UP_LIFT_NAME = "BigRockUpLift";

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

        public override void SetAttackData(BossEnemy.Attack.AttackData attackData)
        {
            _attackData = new BossEnemy.Attack.AttackData(
                        attackData.ID,
                        attackData.Name,
                        attackData.Damage,
                        _attackAreaCircleRadius,
                        attackData.AttackStartDistance,
                        DamageReactionType.Large,
                        attackData.CoolTime);
        }

        [Header("攻撃のEffect再生から当たり判定を開始するまでの時間")]
        [SerializeField] private float _attackHitTimingDelayTime;

        [Header("攻撃の範囲エフェクトの生成位置の高さ")]
        [SerializeField] private float _attackAreaCircleGeneratePosY = 0.2f;

        [Header("ボスから正面の攻撃位置までの距離")]
        [SerializeField] private float _attackPointDistance;

        [Header("ボスエネミーのPhaseChangeEffectの個数")]
        [SerializeField] private int _phaseChangeEffectNum = 6;

        [Header("ボスエネミーのPhaseChangeEffectのボスからの距離")]
        [SerializeField] private float _phaseChangeEffectSpawnDistance = 6;

        [Header("攻撃範囲の半径")]
        [SerializeField] private float _attackAreaCircleRadius = 6;

        protected async override UniTask PlayAttack(CancellationToken cancellationToken)
        {
            // 攻撃位置を取得
            var attackCenterPos = _bossCharacterTransform.position + _bossCharacterTransform.forward * _attackPointDistance;
            attackCenterPos.y = _attackAreaCircleGeneratePosY;

            // 攻撃範囲を表示
            HitAreaView hitArea = _attackHitAreaSpawner.Spawn(
                AttackHitAreaType.Circle,
                attackCenterPos,
                _attackAreaCircleRadius);

            _visibleHitAreaList.Add(hitArea);

            // 攻撃のEffectは発生するまでの時間
            var playEffectStartTime = _elapsedTime + _attackAreaDespawnAndDisplayAttackEffectTime;

            // 攻撃範囲が生成されてから攻撃が行われるまでの時間待機
            await UniTask.WaitUntil(() =>
                _elapsedTime >= playEffectStartTime,
                cancellationToken: cancellationToken);

            // 攻撃範囲を見えなくする
            hitArea.InVisible();
            _visibleHitAreaList.Remove(hitArea);

            // 攻撃音の再生
            PlayBossSE(SoundCueNames.Boss.RockEruption);

            var effectCenterPos = _bossCharacterTransform.position;
            effectCenterPos.y = _attackAreaCircleGeneratePosY;

            Vector3[] effectSpawnPosArray =
            CirclePositionGenerator.GetPositionsOnCircle3D(
                    effectCenterPos,
                    _phaseChangeEffectSpawnDistance,
                    _phaseChangeEffectNum);

            foreach (var effectPos in effectSpawnPosArray)
            {
                _effectManager.PlayEffect(BIG_ROCK_UP_LIFT_NAME, effectPos);
            }

            // 攻撃Effectを再生
            _effectManager.PlayEffect(IMPACT_EFFECT, attackCenterPos);

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
