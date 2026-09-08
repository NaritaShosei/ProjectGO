using System;
using UnityEngine;
 
namespace BossEnemy.Interface
{
    public interface IBossCharacterAnimationEventReceiver
    {
        /// <summary>Animation中に動く際のイベント(目的地と到達までの時間)</summary>
        public event Action<Vector3, float> OnMoveCharacter;

        /// <summary>BossEnemyのIsTriggerのOnOffを切り替えるイベント</summary>
        public event Action<bool> OnColliderIsTriggerIsEnabled;

        /// <summary> 
        /// 攻撃当たり判定を行うタイミングのイベント
        /// AttackHitAreaType = 攻撃の当たり判定の種類
        /// Vector3 = 当たり判定の大きさ
        /// Vector3 = 当たり判定を行う方向
        /// </summary>
        public event Action<AttackHitAreaType, Vector3, Vector3> OnCheckHitAttack;

        /// <summary> 攻撃が当たった際のイベント </summary>
        public event Action OnHitAttack;

        /// <summary>攻撃アニメーション終了のイベント</summary>
        public event Action OnAttackEnd;

        /// <summary>Phase切り替え終了のイベント</summary>
        public event Action OnPhaseChangeEnd;

        /// <summary> 姿勢の切り替え完了時イベント </summary>
        public event Action OnPostureChangeCompleted;

        /// <summary>死亡アニメーション終了のイベント</summary>
        public event Action OnDeadEnd;

        /// <summary>AttackSMB から移動開始タイミングで呼ばれる</summary>
        public void AnimEvent_MoveCharacter(Vector3 goal, float time);

        /// <summary> BossEnemyのIsTriggerのOnOffを切り替える </summary>
        public void AnimEvent_ColliderIsTriggerIsEnabled(bool isTrigger);

        /// <summary> AttackSMB から攻撃当たり判定を行うタイミングで呼ばれる </summary>
        public void AnimEvent_AttackHitCheck(AttackHitAreaType attackHitAreaType, Vector3 attackPosition, Vector3 forward = default);

        /// <summary> 攻撃が当たった際に呼ばれる </summary>
        public void AnimEvent_HitAttack();

        /// <summary>AttackSMB からステート終了時に呼ばれる</summary>
        public void AnimEvent_AttackEnd();

        /// <summary>PhaseChangeSMB からステート終了時に呼ばれる</summary>
        public void AnimEvent_PhaseChangeEnd();

        /// <summary> 姿勢の切り替え完了時に呼ばれる </summary>
        public void AnimEvent_PostureChangeCompleted();

        /// <summary>DeadSMB からステート終了時に呼ばれる</summary>
        public void AnimEvent_DeadEnd();
    }
}
