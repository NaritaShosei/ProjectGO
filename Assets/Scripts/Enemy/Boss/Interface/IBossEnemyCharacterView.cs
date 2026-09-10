using BossEnemy.Animation;
using BossEnemy.Armor;
using BossEnemy.Character;
using BossEnemy.Enum;
using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

namespace BossEnemy.Interface
{
    public interface IBossEnemyCharacterView : IEnemy, IPoolable, ISpeedChange
    {
        /// <summary>ダメージを受けたときに発火するイベント</summary>
        public event Action<DamageContext, TakeDamageType, ArmorAttachmentType> OnTakeDamage;

        /// <summary> 姿勢変更後発火されるイベント </summary>
        public event Action<PostureType> OnChangedPosture;

        /// <summary>ロックオン可能なパーツが変わった際のイベント<新しいターゲット、古いターゲット></summary>
        public event Action<(IReadOnlyList<ILockOnTarget> newTargetParts, IReadOnlyList<ILockOnTarget> oldTargetParts)> OnChangeLockOnParts;

        /// <summary> 行動開始イベント </summary>
        public event Action OnBeginsAction;

        /// <summary> 現在攻撃可能なボスの部位 </summary>
        public BossCharacterPartsView[] ActiveBossEnemyPartsView { get; }

        /// <summary> 回転をセットする </summary>
        public void SetRotation(Quaternion quaternion);

        /// <summary> 速度をセットする </summary>
        public void SetVelocity(Vector3 velocity);

        /// <summary> 行動開始処理 </summary>
        public void StartAction();

        /// <summary> 攻撃処理 </summary>
        public void ExecuteAttack(Attack.AttackData bossEnemyAttackData);

        /// <summary> 攻撃終了処理 </summary>
        public void AttackCompleted();

        /// <summary> フェーズ変更・死亡などによって中断された攻撃処理を停止する </summary>
        public void StopActiveAttacks();

        /// <summary> フェーズ切り替え処理 </summary>
        public void ChangePhase(int nextPhase);

        /// <summary> キャラクターの姿勢を変更 </summary>
        /// <param name="postureType"></param>
        public void ChangePosture(PostureType postureType);

        /// <summary> タイムスケールの数値変更時に発火するReactiveProperty </summary>
        public IReadOnlyReactiveProperty<float> TimeScaleReactiveProperty { get; }

        /// <summary> 死亡イベント発生時の処理 </summary>
        public void HandleDead();

        #region 鎧関連の処理
        public void InitArmor();

        public void BreakArmor(ArmorAttachmentType attachmentPointsType);

        public void RepairArmor(ArmorAttachmentType attachmentPointsType);
        #endregion
    }
}
