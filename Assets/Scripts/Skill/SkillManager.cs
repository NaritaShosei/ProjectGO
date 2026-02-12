using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SkillManager : MonoBehaviour
{
    public event Action<SkillBase> OnSkillAcquired;

    /// <summary> スキルのIDを登録し、獲得時効果を適用する </summary>
    public bool TryRegisterSkillId(int id, IAttackStats stats)
    {
        if (!_skillAcquireCounts.ContainsKey(id))
        {
            _skillAcquireCounts[id] = 0;
        }

        _skillAcquireCounts[id]++;

        // スキルを取得して獲得時効果を適用
        var skill = _skillDataBase.GetSkill(id);
        if (skill != null && skill.Timing == SkillTiming.OnAcquire)
        {
            skill.OnAcquire(stats, _skillAcquireCounts[id]);
            OnSkillAcquired?.Invoke(skill);
        }

        // 次のレベルに進めない場合はスキルが出てこないようにする
        if (skill == null || !skill.CanAcquire(_skillAcquireCounts[id]))
        {
            _ownedSkillIDs.Add(id);
        }

        return true;
    }


    /// <summary> 開放済みのIDを取得する </summary>
    public IReadOnlyList<int> GetOwnedSkillIDs() => _ownedSkillIDs.ToList();

    /// <summary> 攻撃時に適用するスキルのみを取得 </summary>
    public IEnumerable<SkillBase> GetAttackSkills()
    {
        foreach (var id in _ownedSkillIDs)
        {
            var skill = _skillDataBase.GetSkill(id);

            if (skill != null && skill.Timing == SkillTiming.OnAttack)
            {
                yield return skill;
            }
        }
    }

    /// <summary> 取得済みスキルの列挙を返す </summary>
    public IEnumerable<SkillBase> GetOwnedSkills()
    {
        foreach (var id in _ownedSkillIDs)
        {
            var skill = _skillDataBase.GetSkill(id);

            if (skill != null)
            {
                yield return skill;
            }
        }
    }

    /// <summary> 与えられた数分ランダムにスキルを取得 </summary>
    public List<SkillBase> GetSelectableSkills(int count)
    {
        if (_skillDataBase == null)
        {
            Debug.LogWarning("SkillDataBaseが未設定です");
            return new List<SkillBase>();
        }

        return _skillDataBase.GetAllSkills()
            .Where(s => !_ownedSkillIDs.Contains(s.ID))
            .OrderBy(_ => UnityEngine.Random.value)
            .Take(count)
            .ToList();
    }


    [SerializeField] private SkillDataBase _skillDataBase;

    private HashSet<int> _ownedSkillIDs = new HashSet<int>();
    private Dictionary<int, int> _skillAcquireCounts = new();
}
