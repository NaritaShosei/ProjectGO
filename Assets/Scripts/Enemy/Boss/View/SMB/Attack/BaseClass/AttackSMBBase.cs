using UnityEngine;

// BossEnemy関連
using BossEnemy.Data;
using BossEnemy.Model.Interface;
using BossEnemy.Model.System;
using Cysharp.Threading.Tasks;

namespace BossEnemy.View.SMB
{
    public class AttackSMBBase : StateMachineBehaviour
    {
        public void Init(
            IAnimationEventReceiver bossEnemyAnimationEventReceiver,
            BossEnemyAnimator bossAnimator,
            AttackInformationHolder attackInformation,
            CameraManager cameraManager,
            IAttackHitAreaSpawner attackHitAreaSpawner,
            Transform bossEnemyTransform, IPlayer player)
        {
            _animationEventReceiver = bossEnemyAnimationEventReceiver;
            _bossAnimator = bossAnimator;
            _informationHolder = attackInformation;
            _cameraManager = cameraManager;
            _attackHitAreaSpawner = attackHitAreaSpawner;
            _bossEnemyTransform = bossEnemyTransform;
            _target = player;
        }

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            _attackData = _informationHolder.AttackData;
            _hitCenterPos = _bossEnemyTransform.position + 
                (_bossEnemyTransform.forward * _attackData.AttackHitAreaCenterDistance);
            _isAnimRunning = true;
            _isChargeCompleted = false;
            _attackHitFired = false;
            _isAttackAreaActive = false;
            _stateLength = stateInfo.length;
            PlayBossSE(AttackStartVoiceCueName);
            Debug.Log("アニメーション合計時間：" + stateInfo.length);
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            // 自ステートへの遷移中、遷移元の古いステートからの OnStateUpdate が
            // 共有メンバ変数を上書きしてリセットを汚染するのを防ぐため、処理をスキップする
            if (animator.IsInTransition(layerIndex) && stateInfo.normalizedTime >= 0.5f) return;

            // アニメーション開始からの経過秒数計測
            _elapsedSeconds = stateInfo.normalizedTime * _stateLength;

            AttackSequence(animator, stateInfo, layerIndex);

            if (_isAnimRunning)
            {
                if (_stateLength <= _elapsedSeconds)
                {
                    _bossAnimator.SetAttacking(false);
                    _isAnimRunning = false;
                }
            }
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            _animationEventReceiver.AnimEvent_AttackEnd();
        }

        public virtual void AttackSequence(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            // 攻撃
            if (!_isChargeCompleted && _attackData.AttackChargeTime < _elapsedSeconds)
            {
                PlayBossSE(AttackCueName);
                _attackHitFired = true;
                _isChargeCompleted = true;
            }


            if(!_isAttackAreaActive && _elapsedSeconds > _attackData.AttackAreaEffectStartTime)
            {
                _attackHitAreaSpawner.Spawn(HitAreaType.Circle, _hitCenterPos, _attackData.AttackRange, _attackAreaDespawnTime);
                _isAttackAreaActive = true;
            }


            if (_attackHitFired)
            {
                _cameraManager.ExecutionCameraShake(_cameraShakeData).Forget();
                if (AttackHitChecker.TryHitAttack(HitAreaType.Circle, _hitCenterPos, _target, _attackData.AttackRange))
                {
                    _animationEventReceiver.AnimEvent_AttackHit();
                    _attackHitFired = false;
                }

                if (_attackData.AttackChargeTime + _attackData.AttackDuration < _elapsedSeconds)
                {
                    _attackHitFired = false;
                }
            }
        }

        [SerializeField] protected float _attackAreaDespawnTime = 1.0f;

        [Header("攻撃時のCameraShakeData")]
        [SerializeField] protected CameraShakeData _cameraShakeData;

        protected virtual string AttackStartVoiceCueName => null;

        protected virtual string AttackCueName => null;

        protected CameraManager _cameraManager = null;

        protected AttackInformationHolder _informationHolder;

        protected BossEnemyAttackData _attackData;

        protected IAnimationEventReceiver _animationEventReceiver = null;

        protected BossEnemyAnimator _bossAnimator = null;

        protected IAttackHitAreaSpawner _attackHitAreaSpawner = null;

        protected Vector3 _hitCenterPos;

        protected float _totalAnimTime = 0;

        protected bool _attackHitFired = false;

        protected bool _isChargeCompleted = false;

        protected bool _isAnimRunning = false;

        protected bool _isAttackAreaActive = false;

        protected float _elapsedSeconds = 0;

        protected float _stateLength = 0;

        protected Transform _bossEnemyTransform;

        protected IPlayer _target;

        protected void PlayBossSE(string cueName)
        {
            if (string.IsNullOrEmpty(cueName)) return;
            if (_bossEnemyTransform == null) return;

            Sound.PlaySE(_bossEnemyTransform.gameObject, cueName, CueSheetType.Boss);
        }
    }
}
