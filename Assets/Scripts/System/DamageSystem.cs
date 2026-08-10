using UnityEngine;

// Boss関連
using BossEnemy.Enum;
using BossEnemy.Character;

public class DamageSystem
{
    const float DAMAGE_REDUCTION_RATE_BASE = 0.01f;

    const int DEFENSE_CONSTANT = 100;
    const int MIN_DAMAGE = 1;

    const float WEEK_POINT_DAMAGE = 1.5f;
    const float DECREASE_DAMAGE = 0.8f;

    public static int CalculateDamage(
        DamageContext attack,
        EnemyDefenseContext defense)
    {
        // クリティカルならその分の攻撃力をAttackPowerに上乗せする
        if (attack.IsCritical) attack.AttackPower = GetCriticalAttackPower(attack);

        // 感電デバフ
        if (defense.HasShockDebuff)
        {
            float upDamage = attack.AttackPower * attack.ElectricShock.UpDamagePercentage;
            attack.AttackPower += upDamage;
        }

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
        float damageReductionRate = DAMAGE_REDUCTION_RATE_BASE * bodyDefense;

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
        switch (mode)
        {
            case PlayerMode.Warrior:
                switch (type)
                {
                    case EnemyDefenceType.Armor: return WEEK_POINT_DAMAGE;
                    case EnemyDefenceType.Flesh: return DECREASE_DAMAGE;
                }
                break;

            case PlayerMode.Thunder:
                switch (type)
                {
                    case EnemyDefenceType.Armor: return WEEK_POINT_DAMAGE;
                    case EnemyDefenceType.Flesh: return DECREASE_DAMAGE;
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
}
