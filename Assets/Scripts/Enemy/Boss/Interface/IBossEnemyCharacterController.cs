using BossEnemy.AI.BehaviourTree;
using BossEnemy.Character;
using BossEnemy.Enum;
using UnityEngine;

namespace BossEnemy.Interface
{
    public interface IBossEnemyCharacterController : IUpdater
    {
        /// <summary> 初期化処理 </summary>
        public void Init(
            IBossEnemyCharacterView bossEnemyCharacterView,
            EnemyServices enemyServices,
            IBossCharacterAnimationEventReceiver animationEventReceiver,
            ITreeNode entryNode,
            IBossCharacterEntity bossCharacterEntity);

        /// <summary> BehaviourTreeの探索を開始する </summary>
        public void HandleRunningBehaviourTree();

        /// <summary> 死亡イベント発火時の処理 </summary>
        public void HandleDead();

        /// <summary> フェーズ切り替えイベント発火時の処理 </summary>
        public void HandlePhaseChange();

        /// <summary> ボスの体勢が崩れた際のイベント発火時の処理 </summary>
        /// <param name="downPosture"> ダウン後のボスの体勢 </param>
        public void HandleChangePosture(PostureType downPosture);

        /// <summary> 被ダメージイベント発火時の処理 </summary>
        /// <param name="damageContext"> 被ダメージ状況 </param>
        /// <param name="hitPartsType"> 攻撃被弾ヶ所 </param>
        /// <param name="scapegoatArmor"> 被弾ヶ所が鎧装着時に身代わりとなる鎧の部位 </param>
        public void HandleTakeDamage(DamageContext damageContext, TakeDamageType hitPartsType, ArmorAttachmentType scapegoatArmor);

        /// <summary> 移動イベント発火時の処理 </summary>
        public void HandleMovePosition(Vector3 position);

        /// <summary> Characterに回転が加わった際のイベント発火時の処理 </summary>
        public void HandleChangeRotation(Quaternion quaternion);

        /// <summary> 移動によってVelocityの値が変わったときの処理 </summary>
        public void HandleChangeMoveVelocity(Vector3 velocity);

        /// <summary> ボスの攻撃開始イベント発火時の処理 </summary>
        /// <param name="bossEnemyAttackData"> 攻撃データ </param>
        public void HandleAttackStart();

        /// <summary> ボスの攻撃が終了した際のイベント発火時の処理 </summary>
        public void HandleAttackEnd();

        /// <summary> ボスの攻撃がターゲットに当たった際のイベント発火時の処理 </summary>
        public void HandleCheckHitAttack();

        /// <summary> TimeScale変更イベント発火時の処理 </summary>
        public void HandleChangedTimeScale(float timeScale);
    }
}
