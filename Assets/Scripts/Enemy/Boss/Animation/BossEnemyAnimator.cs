using BossEnemy.Enum;
using BossEnemy.Interface;
using System;
using UnityEngine;

namespace BossEnemy.Animation
{
    public class BossEnemyAnimator : IBossEnemyAnimator
    {
        // Receiverから中継するイベント（外部への単一エントリポイント）
        public event Action OnAttackHit;
        public event Action OnAttackEnd;
        public event Action OnDownEnd;
        public event Action OnDeadEnd;
        public event Action OnPhaseChangeEnd;

        /// <summary>
        /// コンストラクタ。ReceiverのイベントをAnimatorへ中継する。
        /// </summary>
        public BossEnemyAnimator(Animator animator, IBossCharacterAnimationEventReceiver receiver)
        {
            _animator = animator;
            _receiver = receiver;

            if (_receiver == null) return;

            _receiver.OnAttackCompleted += HandleAttackEnd;
            _receiver.OnDeadEnd += HandleDeadEnd;
            _receiver.OnPhaseChangeEnd += HandlePhaseChangeEnd;
        }

        /// <summary>
        /// 移動速度を設定する（Idle / Move の切り替えに使用）
        /// </summary>
        public void SetSpeed(float xSpeed, float zSpeed)
        {
            if (_animator == null) return;
            _animator.SetFloat(_hashXSpeed, xSpeed);
            _animator.SetFloat(_hashZSpeed, zSpeed);
        }

        /// <summary>
        /// 攻撃中フラグを設定する
        /// </summary>
        public void SetAttacking(bool value, int attackDataID)
        {
            if (_animator == null) return;

            _animator.SetBool(_hashIsAttacking, value);
            _animator.SetInteger(_hashExecutingAttackID, attackDataID);
        }

        /// <summary>
        /// 各所アーマー破壊フラグを設定する
        /// </summary>
        public void SetPosture(PostureType postureType)
        {
            if (_animator == null) return;
            
            int enumValue = (int)postureType;
            _animator.SetInteger(_hashPostureTypeValue, enumValue);
        }

        /// <summary>
        /// 感電フラグを設定する
        /// </summary>
        public void SetElectrified(bool value)
        {
            if (_animator == null) return;
            _animator.SetBool(_hashIsElectrified, value);
        }

        /// <summary>
        /// 死亡フラグを設定する（一度設定したら戻さない）
        /// </summary>
        public void SetDead()
        {
            if (_animator == null) return;
            _animator.SetBool(_hashIsDead, true);
        }

        public void SetPhaseChange(int nextPhase)
        {
            if (_animator == null) return;
            _animator.SetTrigger(_hashPhaseChange);
            _animator.SetInteger(_hashCurrentPhase, nextPhase);
        }

        /// <summary>
        /// アニメーション再生速度を設定する。
        /// HitStopManager から OnSpeedChange 経由で呼ばれる。
        /// </summary>
        public void SetAnimSpeed(float speed)
        {
            if (_animator == null) return;
            _animator.speed = speed;
        }


        /// <summary>
        /// Receiverのイベント購読を解除する。
        /// EnemyのOnDestroyから呼ぶこと。
        /// </summary>
        public void Dispose()
        {
            if (_receiver == null) return;

            _receiver.OnAttackCompleted -= HandleAttackEnd;
            _receiver.OnDeadEnd -= HandleDeadEnd;
            _receiver.OnPhaseChangeEnd -= HandlePhaseChangeEnd;
        }

        // Animatorパラメータのハッシュ
        private readonly int _hashXSpeed = Animator.StringToHash("Speed_x");
        private readonly int _hashZSpeed = Animator.StringToHash("Speed_z");
        private readonly int _hashPostureTypeValue = Animator.StringToHash("PostureTypeValue");
        private readonly int _hashIsAttacking = Animator.StringToHash("IsAttacking");
        private readonly int _hashExecutingAttackID = Animator.StringToHash("ExecutingAttackID");
        private readonly int _hashIsElectrified = Animator.StringToHash("IsElectrified");
        private readonly int _hashIsDead = Animator.StringToHash("IsDead");
        private readonly int _hashPhaseChange = Animator.StringToHash("PhaseChange");
        private readonly int _hashCurrentPhase = Animator.StringToHash("CurrentPhase");

        private readonly Animator _animator;

        // 購読解除のためにReceiverを保持する
        private readonly IBossCharacterAnimationEventReceiver _receiver;

        private void HandleAttackEnd() => OnAttackEnd?.Invoke();
        private void HandleDeadEnd() => OnDeadEnd?.Invoke();
        private void HandlePhaseChangeEnd() => OnPhaseChangeEnd?.Invoke();
    }
}

