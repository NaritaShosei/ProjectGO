using UnityEngine;

namespace BossEnemy.Attack
{
    /// <summary> BossEnemyの攻撃データ </summary>
    public struct AttackData
    {
        /// <summary> BossEnemyの攻撃データ </summary>
        /// <param name="id"> データID </param>
        /// <param name="name"> 攻撃名称 </param>
        /// <param name="damage"> 攻撃のダメージ </param>
        /// <param name="attackHitAreaRadius"> 攻撃範囲の半径 </param>
        /// <param name="attackStartDistance"> 攻撃を開始できるターゲットとの距離 </param>
        /// <param name="KnockBackLevel"> 攻撃によるノックバックの威力 </param>
        /// <param name="coolTime"> 攻撃終了から次に使えるようになるまでのクールタイム </param>
        /// <param name="animParam"> 攻撃アニメーションのパラメータ名 </param>
        public AttackData(
            int id, string name, float damage, float attackHitAreaRadius, float attackStartDistance, KnockbackLevel KnockBackLevel, float coolTime, string animParam)
        {
            ID = id;
            Name = name;
            Damage = damage;
            AttackHitAreaRadius = attackHitAreaRadius;
            AttackStartDistance = attackStartDistance;
            KnockBackPower = KnockBackLevel;
            CoolTime = coolTime;
            AnimParamName = animParam;
        }

        /// <summary> データID </summary>
        public readonly int ID;

        /// <summary> 攻撃名称 </summary>
        public readonly string Name;

        /// <summary> 攻撃のダメージ量 </summary>
        public readonly float Damage;

        /// <summary> 攻撃範囲 </summary>
        public readonly float AttackHitAreaRadius;

        /// <summary> 攻撃を開始できるターゲットとの距離 </summary>
        public readonly float AttackStartDistance;

        /// <summary> 攻撃によるノックバックの威力 </summary>
        public readonly KnockbackLevel KnockBackPower;

        /// <summary> 攻撃終了から次に使えるようになるまでのクールタイム </summary>
        public readonly float CoolTime;

        /// <summary> アニメーションのパラメータ名 </summary>
        public readonly string AnimParamName;
    }
}
