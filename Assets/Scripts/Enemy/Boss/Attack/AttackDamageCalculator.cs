using Unity.AppUI.UI;
using UnityEngine;

namespace BossEnemy.Attack
{
    /// <summary> ボスの攻撃計算機 </summary>
    public class AttackDamageCalculator
    {
        private const int AttackPowerDivisor = 100;

        /// <summary> ボスの攻撃のダメージ計算処理 </summary>
        /// <param name="attackPower"> 攻撃力 </param>
        /// <param name="damage"> 技のダメージ </param>
        /// <returns>  </returns>
        public static int AttackDamageCalculate(int attackPower, int damage)
        {
            var totalDamage = damage * (attackPower / AttackPowerDivisor);

            return totalDamage;
        }
    }
}
