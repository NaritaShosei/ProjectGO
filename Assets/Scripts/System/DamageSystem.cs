using UnityEngine;

public static class DamageSystem
{
    public static int Calculate(
        DamageContext attack,
        DefenseContext defense)
    {
        float damage =
            GetCriticalDamage(attack, defense)
          * GetEnemyTypeMultiplier(attack.PlayerMode, defense.EnemyType);

        return Mathf.RoundToInt(damage);
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

    private static float GetCriticalDamage(DamageContext attack, DefenseContext defense)
    {
        if (!attack.IsCritical) { return attack.AttackPower; }

        else
        {
            return attack.AttackPower * attack.CriticalMultiplier;
        }
    }
}
