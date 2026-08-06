using UnityEngine;

namespace BossEnemy.Attack
{
    /// <summary> BossEnemyの攻撃データ </summary>
    public struct AttackData
    {
        /// <summary> BossEnemyの攻撃データ </summary>
        /// <param name="id"> データID </param>
        /// <param name="name"> 攻撃名称 </param>
        /// <param name="attackChargeTime"> 攻撃までの溜め時間 </param>
        /// <param name="attackDuration"> 攻撃当たり判定持続時間 </param>
        /// <param name="recoveryTime"> 攻撃後硬直時間 </param>
        /// <param name="attackAreaEffectStartTime"> 攻撃開始から範囲エフェクト発生までの時間 </param>
        /// <param name="damage"> 攻撃のダメージ </param>
        /// <param name="attackRange"> 攻撃範囲 </param>
        /// <param name="attackHitAreaCenterDistance"> 攻撃範囲の中心部 </param>
        /// <param name="attackStartDistance"> 攻撃を開始できるターゲットとの距離 </param>
        /// <param name="nockBackPower"> 攻撃によるノックバックの威力 </param>
        /// <param name="coolTime"> 攻撃終了から次に使えるようになるまでのクールタイム </param>
        /// <param name="animParam"> 攻撃アニメーションのパラメータ名 </param>
        public AttackData(
            int id, string name, float attackChargeTime, float attackDuration, 
            float recoveryTime, float attackAreaEffectStartTime, float damage, 
            float attackRange, float attackHitAreaCenterDistance, float attackStartDistance, 
            float nockBackPower, float coolTime, string animParam)
        {
            ID = id;
            Name = name;
            AttackChargeTime = attackChargeTime;
            AttackDuration = attackDuration;
            RecoveryTime = recoveryTime;
            AttackAreaEffectStartTime = attackAreaEffectStartTime;
            Damage = damage;
            AttackRange = attackRange;
            AttackHitAreaCenterDistance = attackHitAreaCenterDistance;
            AttackStartDistance = attackStartDistance;
            NockBackPower = nockBackPower;
            CoolTime = coolTime;
            AnimParamName = animParam;
        }

        /// <summary> データID </summary>
        public readonly int ID;

        /// <summary> 攻撃名称 </summary>
        public readonly string Name;

        /// <summary> 攻撃までの溜め時間 </summary>
        public readonly float AttackChargeTime;

        /// <summary> 攻撃当たり判定持続時間 </summary>
        public readonly float AttackDuration;

        /// <summary> 攻撃後硬直時間 </summary>
        public readonly float RecoveryTime;

        /// <summary> 攻撃開始から範囲エフェクト発生までの時間 </summary>
        public readonly float AttackAreaEffectStartTime;

        /// <summary> 攻撃のダメージ量 </summary>
        public readonly float Damage;

        /// <summary> 攻撃範囲 </summary>
        public readonly float AttackRange;

        /// <summary> 攻撃範囲の中心部 </summary>
        public readonly float AttackHitAreaCenterDistance;

        /// <summary> 攻撃を開始できるターゲットとの距離 </summary>
        public readonly float AttackStartDistance;

        /// <summary> 攻撃によるノックバックの威力 </summary>
        public readonly float NockBackPower;

        /// <summary> 攻撃終了から次に使えるようになるまでのクールタイム </summary>
        public readonly float CoolTime;

        /// <summary> アニメーションのパラメータ名 </summary>
        public readonly string AnimParamName;
    }
}
