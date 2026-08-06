using BossEnemy.Armor;
using BossEnemy.Attack;
using BossEnemy.Character;
using BossEnemy.Enum;
using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

namespace BossEnemy.System 
{ 

    #region 攻撃処理
    /// <summary>  </summary>
    public class Attack
    {
        public static void AttackActionStart()
        {
            
        }

        public static void Hit(IHealth target)
        {

        }

        public static void Finish()
        {

        }
    }
    #endregion

    #region 移動ロジック
    /// <summary> IMoveTarget継承オブジェクト用の移動ロジック </summary>
    public class Movement
    {
        /// <summary> 最終移動目標地点までの1フレーム内での移動距離を算出し移動対象を動かす処理 </summary>
        /// <param name="moveTarget"> 移動対象 </param>
        /// <param name="targetPos"> 最終移動目標地点 </param>
        /// <param name="moveSpeed"> 移動速度 </param>
        /// <param name="timeScale"> タイムスケール </param>
        public static void MoveTargetPosition(
            IMoveTarget moveTarget, Vector3 targetPos, float moveSpeed, float timeScale = 1)
        {
            // speed * Time.deltaTime で「1フレームあたりの移動量」にする
            float oneFrameSpeed = moveSpeed * Time.deltaTime;
            oneFrameSpeed *= timeScale;

            // Y座標を動かさずに移動距離を算出
            float savePosY = moveTarget.Position.Value.y;
            Vector3 movePosition = Vector3.MoveTowards(moveTarget.Position.Value, targetPos, oneFrameSpeed);
            movePosition.y = savePosY;

            // 移動速度を算出
            const float powerAdjustment = 1000; // 移動速度の力の補正をする
            float velocityX = Mathf.Abs(moveTarget.Position.Value.x - movePosition.x) * powerAdjustment;
            float velocityZ = Mathf.Abs(moveTarget.Position.Value.z - movePosition.z) * powerAdjustment;

            // speed * Time.deltaTime で「1フレームあたりの移動量」にする
            moveTarget.SetPosition(movePosition);

            // ターゲット方向への向きを算出
            Vector3 direction = targetPos - moveTarget.Position.Value;

            // 高度差を無視して水平な向きにする
            direction.y = 0;

            // ターゲット方向へターゲットの向いている方向を変更
            if (direction != Vector3.zero)
            {
                Quaternion rotation = Quaternion.LookRotation(direction);
                moveTarget.SetRotation(rotation);
            }

            // 移動速度設定
            Vector3 moveVelocity = new Vector3(velocityX, 0, velocityZ);
            moveTarget.SetVelocity(moveVelocity);
        }

        /// <summary> 最終移動目標地点まで移動目標時間ちょうどにつくための1フレーム内での移動距離を算出し移動対象を動かす処理 </summary>
        /// <param name="moveTarget"> 移動対象 </param>
        /// <param name="targetPos"> 最終移動目標地点 </param>
        /// <param name="time"> 移動目標時間 </param>
        /// <param name="timeScale"> タイムスケール </param>
        public static void MoveTargetPositionRightOnTime(
            IMoveTarget moveTarget, Vector3 targetPos, float time, float timeScale = 1)
        {
            // 既に目標にいる場合は処理を終了する
            if (time <= 0f) return;

            // 残りの距離と方向（ベクトル）を計算
            Vector3 remainingDirection = targetPos - moveTarget.Position.Value;

            //「残りの距離 ÷ 残りの時間」で、今出すべき速度（秒速ベクトル）を逆算
            Vector3 requiredVelocity = remainingDirection / time;

            // 速度に1フレームの時間をかけて、このフレームの移動量を計算
            Vector3 frameMovement = requiredVelocity * Time.deltaTime * timeScale;

            // 現在の座標に移動量を足した「到達すべき座標」を返す
            moveTarget.SetPosition(moveTarget.Position.Value + frameMovement);

            // ターゲット方向への向きを算出
            Vector3 direction = targetPos - moveTarget.Position.Value;

            // 高度差を無視して水平な向きにする
            direction.y = 0;

            // ターゲット方向へターゲットの向いている方向を変更
            if (direction != Vector3.zero)
            {
                Quaternion rotation = Quaternion.LookRotation(direction);
                moveTarget.SetRotation(rotation);
            }
        }

        /// <summary> 対象の方角を一定の速度で </summary>
        /// <param name="moveTarget"> 移動対象 </param>
        /// <param name="targetPos"> 振り向き対象 </param>
        /// <param name="lookSpeed"> 振り向き速度 </param>
        /// <param name="finishAngleThreshold"> 振り向き対象の方角を向いていると判定できる角度の最低誤差 </param>
        /// <param name="isLookingAtTarget"> 振り向き対象の方角を向いているフラグ </param>
        /// <param name="timeScale"> タイムスケール </param>
        public static void LookAtTarget(
            IMoveTarget moveTarget, 
            Vector3 targetPos, 
            float lookSpeed, 
            float finishAngleThreshold, 
            out bool isLookingAtTarget,
            float timeScale = 1)
        {
            // ターゲットへの方向ベクトルを計算（自身の位置からターゲットの位置を引く）
            Vector3 direction = targetPos - moveTarget.Position.Value;

            // 移動速度設定
            Vector3 moveVelocity = new Vector3(Mathf.Abs(direction.x), 0, Mathf.Abs(direction.z));
            moveTarget.SetVelocity(moveVelocity);

            // 上下の傾き（Y成分）を無視して、水平方向の回転のみにする
            direction.y = 0f;

            // 方向ベクトルがゼロ（真上や全く同じ位置）でないかチェック
            if (direction.sqrMagnitude > 0.001f)
            {
                // 向きたい方向のクォータニオン（回転情報）を計算
                Quaternion targetRotation = Quaternion.LookRotation(direction);

                // 現在の回転から目標の回転へ、Time.deltaTimeをかけてゆっくり補間
                moveTarget.SetRotation(Quaternion.Slerp(moveTarget.Rotation.Value, targetRotation, lookSpeed * Time.deltaTime * timeScale));
                float angleDiff = Quaternion.Angle(moveTarget.Rotation.Value, targetRotation);

                // 角度の差がしきい値以下になったかチェック
                if (angleDiff <= finishAngleThreshold)
                {
                    isLookingAtTarget = true;
                    return;
                }
            }

            // ここに到達した場合角度の差がしきい値以下になってないのでisLookingAtTargetをFalseにする
            isLookingAtTarget = false;
        }
    }
    #endregion

    #region ダメージ計算処理
    /// <summary> BossCharacterへのダメージロジック </summary>
    public class Damage
    {
        /// <summary> ダメージを受けた際に呼ばれるメソッド </summary>
        public static void TakeDamage(
            BossCharacterEntity target, 
            DamageContext damageContext, 
            TakeDamageType hitDefenseType, 
            ArmorAttachmentType attachmentArmor)
        {
            if (attachmentArmor == ArmorAttachmentType.None)
            {
                TakeDamageInBody(target, damageContext, hitDefenseType);
            }
            else
            {
                TakeDamageInAttachmentArmor(target, damageContext, attachmentArmor);
            }
        }

        /// <summary> Boss本体へのダメージ処理 </summary>
        private static void TakeDamageInBody(
            BossCharacterEntity target,
            DamageContext damageContext,
            TakeDamageType damageType)
        {
            int defense = 0;
            int damage = 0;

            // 受けて個所によって防御力(肉質)を取得
            defense = target.GetBodyDefense(damageType);

            // Damageを計算
            if (damageType == TakeDamageType.VitalPoint
                || damageType == TakeDamageType.WeekPoint)
            {
                // 弱点への攻撃ならPlayerもModeによるダメージの減増を行う
                damage = DamageSystem.CalculateDamage(defense, damageContext, true, EnemyDefenceType.Flesh);
            }
            else damage = DamageSystem.CalculateDamage(defense, damageContext);

            Debug.Log("本体にダメージ！：" + damage);
            target.TakeDamage(damage);
        }

        private static void TakeDamageInAttachmentArmor(
            BossCharacterEntity target,
            DamageContext damageContext,
            ArmorAttachmentType attachmentType)
        {
            int defense = 0;
            int damage = 0;

            // 防御力を取得
            defense = target.GetArmorStats(attachmentType).Defense;

            // ダメージの総量を計算
            damage = DamageSystem.CalculateDamage(defense, damageContext, true, EnemyDefenceType.Armor);

            // ターゲットにダメージを与える
            target.TakeDamage(damage, attachmentType);
        }
    }
    #endregion
}
