using UnityEngine;

namespace BossEnemy.Attack
{
    /// <summary> ボスの攻撃計算機 </summary>
    public class AttackDamageCalculator
    {
        private const float AttackPowerDivisor = 100;

        /// <summary> ボスの攻撃のダメージ計算処理 </summary>
        /// <param name="attackPower"> 攻撃力 </param>
        /// <param name="damage"> 技のダメージ </param>
        /// <returns>  </returns>
        public static float AttackDamageCalculate(float attackPower, float damage)
        {
            float totalDamage = damage * (attackPower / AttackPowerDivisor);

            return totalDamage;
        }
    }
}
