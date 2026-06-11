using BossEnemy.Data;
using UnityEngine;

public class DamageSystem
{
    const int DEFENSE_CONSTANT = 100;
    const int MIN_DAMAGE = 1;

    public static int CalculateDamage(
        DamageContext attack,
        EnemyDefenseContext defense)
    {
        // クリティカルならその分の攻撃力をAttackPowerに上乗せする
        if (attack.IsCritical) attack.AttackPower = GetCriticalAttackPower(attack);

        // 感電デバフ(仮)
        if (defense.HasShockDebuff) attack.AttackPower *= 1.1f;

        // 合計ダメージを割り出す
        float damage = attack.AttackPower * GetEnemyDefenseTypeMultiplier(attack.PlayerMode, defense.EnemyType);

        // 返り値
        return Mathf.RoundToInt(damage);
    }


    /// <summary> 攻撃のダメージ計算処理 </summary>
    /// <param name="bodyDefense"> Bossの各所の肉質 </param>
    /// <param name="damageContext"> Bossに対する攻撃情報 </param>
    /// <param name="isPlayerModeAddDamage"> Trueの際にPlayerModeによってダメージの変動を行う </param>
    /// <param name="damageHitPlaceType"> ダメージが当たった場所のEnemyDefenseType </param>
    /// <returns> 合計ダメージ </returns>
    public static int CalculateDamage(int bodyDefense, DamageContext damageContext, 
        bool isPlayerModeAddDamage = false, EnemyDefenceType damageHitPlaceType = EnemyDefenceType.Flesh)
    {
        // 合計ダメージの変数
        float totalDamage;

        // PlayerのModeによって発生する追加ダメージ
        float playerModeAddDamage = 1;

        // ダメージの軽減率を割り出す
        float damageReductionRate = 0.01f * bodyDefense;

        // クリティカルならその分の攻撃力をAttackPowerに上乗せする
        if (damageContext.IsCritical) damageContext.AttackPower = GetCriticalAttackPower(damageContext);

        // isPlayerModeAddDamageがTrueならPlayerのModeによってダメージを割合を上下させる
        if (isPlayerModeAddDamage) playerModeAddDamage = GetEnemyDefenseTypeMultiplier(damageContext.PlayerMode, damageHitPlaceType);

        // 合計ダメージを割り出す
        totalDamage = damageContext.AttackPower * damageReductionRate * playerModeAddDamage;

        // 返り値
        return (int)totalDamage;
    }

    public static int ApplyDamageReduction(
     float damage,
     float defensePower)
    {
        float reductionRate =
            defensePower / (defensePower + DEFENSE_CONSTANT);

        return Mathf.RoundToInt(Mathf.Max(
            MIN_DAMAGE, damage * (1f - reductionRate)));
    }

    private static float GetEnemyDefenseTypeMultiplier(PlayerMode mode, EnemyDefenceType type)
    {
        float weekPointDamage = 1.5f;
        float decreaseDamage = 0.8f;

        switch (mode)
        {
            case PlayerMode.Warrior:
                switch (type)
                {
                    case EnemyDefenceType.Armor: return weekPointDamage;
                    case EnemyDefenceType.Flesh: return decreaseDamage;
                }
                break;

            case PlayerMode.Thunder:
                switch (type)
                {
                    case EnemyDefenceType.Armor: return decreaseDamage;
                    case EnemyDefenceType.Flesh: return weekPointDamage;
                }
                break;
        }

        return 1.0f; // 保険
    }

    /// <summary> 攻撃がCriticalの際の攻撃力を渡すメソッド </summary>
    private static float GetCriticalAttackPower(DamageContext attack)
    {
        if (!attack.IsCritical) return attack.AttackPower;
        return attack.AttackPower * attack.CriticalMultiplier;
    }

    /// <summary> BossEnemyの被弾場所の硬度(肉質)を割り出す </summary>
    /// <param name="partsType"> 被弾場所 </param>
    /// <param name="bossEnemyData"> 被弾したBossEnemyのData </param>
    /// <returns> 被弾場所の硬度(肉質) </returns>
    public static int GetHitPartsDefense(BossEnemyPartsType partsType, BossEnemyData bossEnemyData)
    {
        switch (partsType)
        {
            case BossEnemyPartsType.None:
                Debug.LogError("PartsNone");
                break;
            case BossEnemyPartsType.Hard:
                return bossEnemyData.HardSpotsDefense;
            case BossEnemyPartsType.Normal:
                return bossEnemyData.NormalSpotsDefense;
            case BossEnemyPartsType.WeekPoint:
                return bossEnemyData.WeekPointDefense;
            case BossEnemyPartsType.VitalPoint:
                return bossEnemyData.VitalPointDefense;
        }

        return 0;
    }
}
