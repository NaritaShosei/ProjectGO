using Cysharp.Threading.Tasks;
using System;
using UniRx;
using UnityEngine;

using BossEnemy.Enum;
using BossEnemy.Interface;

namespace BossEnemy.Character
{
    public class BossEnemyCharacterController : IBossEnemyCharacterController 
    {
        public void Init(IBossEnemyCharacterView bossEnemyCharacterView, EnemyServices enemyServices, IAnimationEventReceiver animationEventReceiver)
        {

        }

        public void OnUpdate()
        {

        }

        /// <summary> 死亡イベント発火時の処理 </summary>
        public void HandleDead()
        {

        }

        /// <summary> フェーズ切り替えイベント発火時の処理 </summary>
        public void HnadlePhaseChange()
        {

        }

        /// <summary> ボスの体勢が崩れた際のイベント発火時の処理 </summary>
        /// <param name="downPosture"> ダウン後のボスの体勢 </param>
        public void HandleDown(PostureType downPosture)
        {

        }

        /// <summary> ボスの体勢が立て直された際のイベント発火時の処理 </summary>
        /// <param name="wakeUpPosture"> 起き上がった際のボスの体勢 </param>
        public void HandleWakeUp(PostureType wakeUpPosture)
        {

        }

        /// <summary> 被ダメージイベント発火時の処理 </summary>
        /// <param name="damageContext"> 被ダメージ状況 </param>
        /// <param name="hitPartsType"> 攻撃被弾ヶ所 </param>
        /// <param name="scapegoatArmor"> 被弾ヶ所が鎧装着時に身代わりとなる鎧の部位 </param>
        public void HandleTakeDamage(DamageContext damageContext, BodysDefensesType hitPartsType, ArmorAttachmentType scapegoatArmor)
        {

        }

        /// <summary> 移動イベント発火時の処理 </summary>
        /// <param name="velocity"> 移動速度 </param>
        /// <param name="position"> 現在地 </param>
        /// <param name="rotation"> 向いている方向</param>
        public void HandleMove(Vector3 velocity, Vector3 position, Quaternion rotation)
        {

        }

        /// <summary> ボスの攻撃開始イベント発火時の処理 </summary>
        /// <param name="bossEnemyAttackData"> 攻撃データ </param>
        public void HandleAttackStart(BossEnemyAttackData bossEnemyAttackData)
        {

        }

        /// <summary> ボスの攻撃が終了した際のイベント発火時の処理 </summary>
        public void HandleAttackEnd()
        {

        }

        /// <summary> ボスの攻撃がターゲットに当たった際のイベント発火時の処理 </summary>
        public void HandleAttackHit()
        {

        }
    }
}
