using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AttackDataRepository", menuName = "GameData/AttackDataRepository")]
public class AttackDataRepository : ScriptableObject
{
    /// <summary>
    /// IDを基に攻撃データを検索
    /// </summary>
    public AttackData_main GetAttackById(int attackId)
    {
        if (_attackCacheIDBase == null) BuildCache();

        if (_attackCacheIDBase.TryGetValue(attackId, out AttackData_main data))
        {
            return data;
        }

        return null;
    }

    /// <summary>
    /// 与えられた攻撃の内容を基に一致する攻撃を検索
    /// </summary>
    public AttackData_main GetAttackData(
        PlayerMode mode,
        AttackType type,
        int comboIndex,
        ChargeLevel charge)
    {
        // 初回アクセス時に辞書登録
        if (_attackCache == null)
        {
            BuildCache();
        }

        string key = GetCacheKey(mode, type, comboIndex, charge);

        if (_attackCache.TryGetValue(key, out AttackData_main data))
        {
            return data;
        }

        return null;
    }

    [SerializeField] private List<AttackData_main> _attackDatabase;

    // キャッシュ用Dictionary
    private Dictionary<string, AttackData_main> _attackCache;
    private Dictionary<int, AttackData_main> _attackCacheIDBase;

    private void BuildCache()
    {
        _attackCache = new();
        _attackCacheIDBase = new();

        foreach (var attack in _attackDatabase)
        {
            if (attack == null) { continue; }

            string key = GetCacheKey(
                attack.Mode,
                attack.AttackType,
                attack.ComboIndex,
                attack.RequiredCharge
            );

            _attackCacheIDBase[attack.AttackId] = attack;
            _attackCache[key] = attack;
        }
    }

    private string GetCacheKey(
        PlayerMode mode,
        AttackType type,
        int comboIndex,
        ChargeLevel charge)
    {
        return $"{mode}_{type}_{comboIndex}_{charge}";
    }
}