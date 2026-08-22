using System;
using UnityEngine;

[CreateAssetMenu(fileName = "DamageSystemSettings", menuName = "GameData/Damage System Settings")]
public class DamageSystemSettings : ScriptableObject
{
    /// <summary>
    /// プレイヤーモードと攻撃対象の防御種別に対応するダメージ倍率を取得する。
    /// </summary>
    /// <param name="mode">攻撃時のプレイヤーモード。</param>
    /// <param name="type">攻撃対象が生身か鎧かを示す防御種別。</param>
    /// <returns>Inspectorで設定されたダメージ倍率。未定義のモードの場合は1。</returns>
    public float GetMultiplier(PlayerMode mode, EnemyDefenceType type)
    {
        switch (mode)
        {
            case PlayerMode.Warrior: return _warrior.GetMultiplier(type);
            case PlayerMode.Thunder: return _thunder.GetMultiplier(type);
            default: return 1f;
        }
    }

    [Header("Mode Damage Multipliers")]
    [SerializeField] private ModeDamageMultipliers _warrior = new()
    {
        Armor = 1.5f,
        Flesh = 0.8f
    };
    [SerializeField] private ModeDamageMultipliers _thunder = new()
    {
        Armor = 1.5f,
        Flesh = 0.8f
    };

    [Serializable]
    private struct ModeDamageMultipliers
    {
        [Min(0f)] public float Armor;
        [Min(0f)] public float Flesh;

        /// <summary>
        /// 防御種別に対応するダメージ倍率を取得する。
        /// </summary>
        public float GetMultiplier(EnemyDefenceType type)
        {
            return type == EnemyDefenceType.Armor ? Armor : Flesh;
        }
    }
}
