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
    public AttackData GetAttackData(PlayerMode mode)
    {
        // モードに応じた最初の攻撃データを返す
        switch (mode)
        {
            case PlayerMode.Warrior:
                return _warriorFirstData;
            case PlayerMode.Thunder:
                return _thunderFirstData;
            default:
                return null;
        }
    }

    /// <summary>
    /// 次のコンボ攻撃を取得する。スキル解放チェックあり。
    /// </summary>
    public AttackData GetNextComboAttack(int currentAttackId, IEnumerable<int> unlockedSkillIds)
    {
        // 現在の攻撃データを取得
        var current = GetAttackById(currentAttackId);
        if (current == null) return null;

        // 差し込み攻撃が存在する場合はそちらを優先して返す
        foreach (var data in _attackDatabase)
        {
            // 差し込み攻撃の条件を満たすかチェック
            // nullチェック
            if (data == null) continue;
            // 差し込み攻撃の起点が現在の攻撃IDと一致するか
            if (data.InsertAfterAttackId != current.AttackId) continue;
            // スキル解放が必要な攻撃の場合、解放されているかチェック
            if (!data.IsUnlockedBySkill) continue;
            // スキル解放が必要な攻撃の場合、解放されているかチェック
            if (unlockedSkillIds == null) continue;
            if (!unlockedSkillIds.Contains(data.RequiredSkillId)) continue;
            return data;
        }

        if (current.NextComboAttackId == -1) return null; // コンボ終了

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
    [SerializeField] private AttackData _warriorFirstData; // 闘神の最初の攻撃データ
    [SerializeField] private AttackData _thunderFirstData; // 雷神の最初の攻撃データ

    // キャッシュ用Dictionary
    private Dictionary<int, AttackData> _attackCacheIDBase;

    private void BuildCache()
    {
        _attackCacheIDBase = new();

        foreach (var data in _attackDatabase)
        {
            if (data == null) { continue; }

            _attackCacheIDBase[data.AttackId] = data;
        }
    }
}
