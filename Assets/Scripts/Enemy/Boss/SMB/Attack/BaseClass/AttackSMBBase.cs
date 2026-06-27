using BossEnemy.Data;
using UnityEngine;


namespace BossEnemy.SMB
{
    public class AttackSMBBase : StateMachineBehaviour
    {
        public void Init(
            BossEnemyAnimationEventReceiver bossEnemyAnimationEventReceiver,
            BossEnemyAnimator bossAnimator,
            AttackInformationHolder attackInformation,
            IAttackHitAreaSpawner attackHitAreaSpawner,
            Transform bossEnemyTransform, IPlayer player)
        {
            _animationEventReceiver = bossEnemyAnimationEventReceiver;
            _bossAnimator = bossAnimator;
            _informationHolder = attackInformation;
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
            Debug.Log("アニメーション合計時間：" + stateInfo.length);
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            // 自ステートへの遷移中、遷移元の古いステートからの OnStateUpdate が
            // 共有メンバ変数を上書きしてリセットを汚染するのを防ぐため、処理をスキップする
            if (animator.IsInTransition(layerIndex) && stateInfo.normalizedTime >= 0.5f) return;

            // アニメーション開始からの経過秒数計測
            _elapsedSeconds = stateInfo.normalizedTime * stateInfo.length;

            AttackSequence();

            if (_isAnimRunning)
            {
                if (stateInfo.length <= _elapsedSeconds)
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

        public virtual void AttackSequence()
        {
            // 攻撃
            if (!_isChargeCompleted && _attackData.AttackChargeTime < _elapsedSeconds)
            {
                _attackHitFired = true;
                _isChargeCompleted = true;
            }


            if(!_isAttackAreaActive && _elapsedSeconds > _attackData.AttackAreaEffectStartTime)
            {
                float despawnTime = (_attackData.AttackChargeTime + _attackData.AttackDuration) - _elapsedSeconds;
                _attackHitAreaSpawner.Spawn(HitAreaType.Circle, _hitCenterPos, _attackData.AttackRange, despawnTime);
                _isAttackAreaActive = true;
            }


            if (_attackHitFired)
            {
                if(AttackHitDetectionSystem.TryHitAttack(HitAreaType.Circle, _hitCenterPos, _target, _attackData.AttackRange))
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

        protected AttackInformationHolder _informationHolder;

        protected BossEnemyAttackData _attackData;

        protected BossEnemyAnimationEventReceiver _animationEventReceiver = null;

        protected BossEnemyAnimator _bossAnimator = null;

        protected IAttackHitAreaSpawner _attackHitAreaSpawner = null;

        protected Vector3 _hitCenterPos;

        protected float _totalAnimTime = 0;

        protected bool _attackHitFired = false;

        protected bool _isChargeCompleted = false;

        protected bool _isAnimRunning = false;

        protected bool _isAttackAreaActive = false;

        protected float _elapsedSeconds = 0;

        protected Transform _bossEnemyTransform;

        protected IPlayer _target;
    }

    public class AttackInformationHolder
    {
        public BossEnemyAttackData AttackData => _attackData;

        public void SetData(BossEnemyAttackData attackData)
        {
            _attackData = attackData;
        }

        private BossEnemyAttackData _attackData;
    }
}
