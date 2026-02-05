using UnityEngine;

public static class DamageSystem
{
    const int DEFENSE_CONSTANT = 100;
    const int MIN_DAMAGE = 1;

    public static int Calculate(
        DamageContext attack,
        EnemyDefenseContext defense)
    {
        float damage =
            GetCriticalDamage(attack, defense)
          * GetEnemyTypeMultiplier(attack.PlayerMode, defense.EnemyType);

        return Mathf.RoundToInt(damage);
    }

    public static float ApplyDamageReduction(
     float damage,
     float defensePower)
    {
        float reductionRate =
            defensePower / (defensePower + DEFENSE_CONSTANT);

        return Mathf.Max(
            MIN_DAMAGE, damage * (1f - reductionRate));
    }

    private static float GetEnemyTypeMultiplier(PlayerMode mode, EnemyType type)
    {
        switch (mode)
        {
            case PlayerMode.Warrior:
                switch (type)
                {
                    case EnemyType.Armor: return 1.5f;
                    case EnemyType.Flesh: return 0.8f;
                }
                break;

            case PlayerMode.Thunder:
                switch (type)
                {
                    case EnemyType.Armor: return 0.8f;
                    case EnemyType.Flesh: return 1.5f;
                }
                break;
        }

        return 1.0f; // 保険
    }

    private static float GetCriticalDamage(DamageContext attack, EnemyDefenseContext defense)
    {
        if (!attack.IsCritical) { return attack.AttackPower; }

        else
        {
            return attack.AttackPower * attack.CriticalMultiplier;
        }
    }
}
