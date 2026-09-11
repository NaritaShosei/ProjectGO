using System;
using UnityEngine;
using BossEnemy.Interface;

/// <summary>
/// アニメーションのイベントを受け取り、外部へ中継する薄いクラス。
/// ロジックは持たず、イベントの中継のみを担当する。
/// IEnemyAnimationControllerを継承したIBossEnemyAnimationControllerを実装し、SMBからanimator.TryGetComponent()で取得される。
/// </summary>

namespace BossEnemy.Infrastructure
{
    public class BossCharacterAnimationEventReceiver : IBossCharacterAnimationEventReceiver
    {
        /// <summary>Animation中に動く際のイベント(目的地と到達までの時間)</summary>
        public event Action<Vector3, float> OnMoveCharacter;

        /// <summary>BossEnemyのIsTriggerのOnOffを切り替えるイベント</summary>
        public event Action<bool> OnColliderIsTriggerIsEnabled;

        /// <summary>攻撃ヒットタイミングのイベント</summary>
        public event Action<AttackHitAreaType, Vector3, Vector3> OnCheckHitAttack;

        /// <summary> 攻撃当たり判定を行うタイミングのイベント </summary>
        public event Action OnHitAttack;

        /// <summary>攻撃アニメーション終了のイベント</summary>
        public event Action OnAttackCompleted;

        /// <summary>Phase切り替え終了のイベント</summary>
        public event Action OnPhaseChangeEnd;

        /// <summary> 姿勢の切り替え完了時イベント </summary>
        public event Action OnPostureChangeCompleted;

        /// <summary>死亡アニメーション終了のイベント</summary>
        public event Action OnDeadEnd;

        /// <summary>AttackSMB から移動開始タイミングで呼ばれる</summary>
        public void AnimEvent_MoveCharacter(Vector3 goal, float time)
        {
            OnMoveCharacter?.Invoke(goal, time);
        }

        /// <summary> BossEnemyのIsTriggerのOnOffを切り替える </summary>
        public void AnimEvent_ColliderIsTriggerIsEnabled(bool isTrigger)
        {
            OnColliderIsTriggerIsEnabled?.Invoke(isTrigger);
        }

        /// <summary>AttackSMB から攻撃ヒットタイミングで呼ばれる</summary>
        public void AnimEvent_AttackHitCheck(AttackHitAreaType attackHitAreaType, Vector3 attackPosition, Vector3 forward = default)
        {
            OnCheckHitAttack?.Invoke(attackHitAreaType, attackPosition, forward);
        }

        /// <summary> 攻撃が当たった際に呼ばれる </summary>
        public void AnimEvent_HitAttack()
        {
            OnHitAttack?.Invoke();
        }

        /// <summary>AttackSMB からステート終了時に呼ばれる</summary>
        public void AnimEvent_AttackCompleted()
        {
            OnAttackCompleted?.Invoke();
        }

        /// <summary>PhaseChangeSMB からステート終了時に呼ばれる</summary>
        public void AnimEvent_PhaseChangeEnd()
        {
            OnPhaseChangeEnd?.Invoke();
        }

        /// <summary> 姿勢の切り替え完了時に呼ばれる </summary>
        public void AnimEvent_PostureChangeCompleted()
        {
            OnPostureChangeCompleted?.Invoke();
        }

        /// <summary>DeadSMB からステート終了時に呼ばれる</summary>
        public void AnimEvent_DeadEnd()
        {
            OnDeadEnd?.Invoke();
        }
    }

}
