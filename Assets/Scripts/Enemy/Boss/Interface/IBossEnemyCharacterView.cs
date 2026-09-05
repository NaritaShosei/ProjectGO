using BossEnemy.Animation;
using BossEnemy.Armor;
using BossEnemy.Character;
using BossEnemy.Enum;
using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

namespace BossEnemy.Interface
{
    public interface IBossEnemyCharacterView : IEnemy, IPoolable, ISpeedChange
    {
        /// <summary>ダメージを受けたときに発火するイベント</summary>
        public event Action<DamageContext, TakeDamageType, ArmorAttachmentType> OnTakeDamage;

        /// <summary>ロックオン可能なパーツが変わった際のイベント<新しいターゲット、古いターゲット></summary>
        public event Action<(IReadOnlyList<ILockOnTarget> newTargetParts, IReadOnlyList<ILockOnTarget> oldTargetParts)> OnChangeLockOnParts;

        /// <summary> 行動開始イベント </summary>
        public event Action OnBeginsAction;

        /// <summary> TimeScaleの変更があったら発火するイベント </summary>
        public event Action<float> OnChangedTimeScale;

        /// <summary> 現在攻撃可能なボスの部位 </summary>
        public BossCharacterPartsView[] ActiveBossEnemyPartsView { get; }

        /// <summary> 回転をセットする </summary>
        public void SetRotation(Quaternion quaternion);

        /// <summary> 速度をセットする </summary>
        public void SetVelocity(Vector3 velocity);

        /// <summary> 行動開始処理 </summary>
        public void StartAction();

        /// <summary> 攻撃処理 </summary>
        public void StartAttack(Attack.AttackData bossEnemyAttackData);

        /// <summary> 攻撃終了処理 </summary>
        public void AttackEnd();

        /// <summary> フェーズ切り替え処理 </summary>
        public void ChangePhase();

        /// <summary> キャラクターの姿勢を変更 </summary>
        /// <param name="postureType"></param>
        public void ChangePosture(PostureType postureType);

        #region 鎧関連の処理
        public void ArmorInit();

        public void ArmorBreak(ArmorAttachmentType attachmentPointsType);

        public void ArmorRepair(ArmorAttachmentType attachmentPointsType);
        #endregion
    }
}
