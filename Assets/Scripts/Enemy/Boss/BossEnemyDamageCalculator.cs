using UnityEngine;

public class BossEnemyDamageCalculator
{
    /// <summary> BossEnemyへの攻撃のダメージ計算処理 </summary>
    /// <param name="bodyDefense"> Bossの各所の肉質 </param>
    /// <param name="damageContext"> Bossに対する攻撃情報 </param>
    /// <returns></returns>
    public static float CalculateTotalDamage(int bodyDefense, DamageContext damageContext)
    {
        // 合計ダメージの変数
        float totalDamage;

        // ダメージの軽減率を割り出す
        float damageReductionRate = 100 / bodyDefense;

        // 合計ダメージを割り出す
        totalDamage = damageContext.AttackPower * damageReductionRate;

        // 返り値
        return totalDamage;
    }
}
