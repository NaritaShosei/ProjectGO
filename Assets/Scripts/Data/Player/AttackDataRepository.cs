using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "AttackDataRepository", menuName = "GameData/AttackDataRepository")]
public class AttackDataRepository : ScriptableObject
{
    /// <summary>
    /// IDを基に攻撃データを検索
    /// </summary>
    public AttackData GetAttackById(int attackId)
    {
        if (_attackCacheIDBase == null) BuildCache();

        if (_attackCacheIDBase.TryGetValue(attackId, out AttackData data))
        {
            return data;
        }

        return null;
    }

    /// <summary>
    /// 与えられた攻撃の内容を基に一致する攻撃を検索
    /// </summary>
    public AttackData GetAttackData(
        PlayerMode mode,
        int comboIndex)
    {
        // 初回アクセス時に辞書登録
        if (_attackCache == null)
        {
            BuildCache();
        }

        string key = GetCacheKey(mode, comboIndex);

        if (_attackCache.TryGetValue(key, out AttackData data))
        {
            return data;
        }

        return null;
    }

    /// <summary>
    /// 次のコンボ攻撃を取得する。スキル解放チェックあり。
    /// </summary>
    public AttackData GetNextComboAttack(int currentAttackId, IEnumerable<int> unlockedSkillIds)
    {
        // 現在の攻撃データを取得
        var current = GetAttackById(currentAttackId);
        if (current == null || current.NextComboAttackId == -1) return null;

        // 次の攻撃データを取得
        var next = GetAttackById(current.NextComboAttackId);
        if (next == null) return null;

        // スキル解放が必要な攻撃の場合、解放されているかチェック
        if (next.IsUnlockedBySkill)
        {
            if (unlockedSkillIds == null) return null;
            if (!unlockedSkillIds.Contains(next.RequiredSkillId)) return null;
        }

        return next;
    }

    [SerializeField] private List<AttackData> _attackDatabase;

    // キャッシュ用Dictionary
    private Dictionary<string, AttackData> _attackCache;
    private Dictionary<int, AttackData> _attackCacheIDBase;

    private void BuildCache()
    {
        _attackCache = new();
        _attackCacheIDBase = new();

        foreach (var attack in _attackDatabase)
        {
            if (attack == null) { continue; }

            string key = GetCacheKey(
                attack.Mode,
                attack.ComboIndex
            );

            _attackCacheIDBase[attack.AttackId] = attack;
            _attackCache[key] = attack;
        }
    }

    /// <summary>
    /// 攻撃の内容を基にキャッシュ用のキーを生成する
    /// </summary>
    private string GetCacheKey(
        PlayerMode mode,
        int comboIndex)
    {
        return $"{mode}_{comboIndex}";
    }
}
