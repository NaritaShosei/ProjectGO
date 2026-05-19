using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AttackDataRepository", menuName = "GameData/AttackDataRepository")]
public class AttackDataRepository : ScriptableObject
{
    [SerializeField] private List<AttackData> _attackDatabase;

    private Dictionary<string, AttackData> _attackCache;
    private Dictionary<int, AttackData> _attackCacheIDBase;

    // 実行時コンボ上書きテーブル (fromId → nextId)
    private Dictionary<int, int> _nextComboOverrides = new();

    public AttackData GetAttackById(int attackId)
    {
        if (_attackCacheIDBase == null) BuildCache();
        _attackCacheIDBase.TryGetValue(attackId, out AttackData data);
        return data;
    }

    public AttackData GetAttackData(
        PlayerMode mode, AttackType type, int comboIndex, ChargeLevel charge)
    {
        if (_attackCache == null) BuildCache();
        string key = GetCacheKey(mode, type, comboIndex, charge);
        _attackCache.TryGetValue(key, out AttackData data);
        return data;
    }

    /// <summary>
    /// 実行時にコンボの次の攻撃IDを上書きする
    /// </summary>
    public void SetNextComboOverride(int fromAttackId, int toAttackId)
    {
        _nextComboOverrides[fromAttackId] = toAttackId;
    }

    /// <summary>
    /// attackIdの次のコンボIDを返す。オーバーライドがあれば優先する
    /// </summary>
    public int GetNextComboAttackId(int attackId)
    {
        if (_nextComboOverrides.TryGetValue(attackId, out int overrideId))
            return overrideId;

        var data = GetAttackById(attackId);
        return data != null ? data.NextComboAttackId : -1;
    }

    /// <summary>
    /// ランタイム状態をリセット (シーン再ロード等)
    /// </summary>
    public void ResetOverrides()
    {
        _nextComboOverrides.Clear();
    }

    private void BuildCache()
    {
        _attackCache = new();
        _attackCacheIDBase = new();
        foreach (var attack in _attackDatabase)
        {
            if (attack == null) continue;
            string key = GetCacheKey(
                attack.Mode, attack.AttackType, attack.ComboIndex, attack.RequiredCharge);
            _attackCacheIDBase[attack.AttackId] = attack;
            _attackCache[key] = attack;
        }
    }

    private string GetCacheKey(
        PlayerMode mode, AttackType type, int comboIndex, ChargeLevel charge)
        => $"{mode}_{type}_{comboIndex}_{charge}";
}
