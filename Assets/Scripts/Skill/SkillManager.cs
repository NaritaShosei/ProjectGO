using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SkillManager : MonoBehaviour
{
    public event Action<SkillBase> OnSkillAcquired;

    public void Init(IPlayerStats stats, IModeController modeController,
        Transform playerTransform, EnemyManager enemyManager)
    {
        _skillExecutor = new SkillExecutor(this, stats, modeController, playerTransform, enemyManager);
    }

    public bool TryRegisterSkillId(int id, IPlayerStats stats)
    {
        if (_skillDataBase == null) return false;
        var skill = _skillDataBase.GetSkill(id);
        if (skill == null) return false;

        if (!_skillAcquireCounts.ContainsKey(id))
            _skillAcquireCounts[id] = 0;
        _skillAcquireCounts[id]++;

        if (skill.Timing == SkillTiming.OnAcquire)
            skill.OnAcquire(stats, _skillAcquireCounts[id]);

        OnSkillAcquired?.Invoke(skill);

        _ownedSkillIDs.Add(id);
        if (!skill.CanAcquire(_skillAcquireCounts[id] + 1))
            _exhaustedSkillIDs.Add(id);

        // Passive スキルは CreateUpdater() で Updater を生成して登録
        // 同じスキルを複数回取得しても Updater は1つだけ登録する
        if (skill.Timing == SkillTiming.Passive && _registeredSkillIDs.Add(id))
        {
            var updater = skill.CreateUpdater();
            if (updater != null)
                _updaters.Add(updater);
        }

        return true;
    }

    public IReadOnlyList<ISkillUpdater> GetUpdaters() => _updaters;
    public IEnumerable<int> GetOwnedSkillIDs() => _ownedSkillIDs;
    public IEnumerable<SkillBase> GetAttackSkills() => GetSkillsByTiming(SkillTiming.OnAttack);

    public IEnumerable<SkillBase> GetOwnedSkills()
    {
        foreach (var id in _ownedSkillIDs)
        {
            var skill = _skillDataBase.GetSkill(id);
            if (skill != null) yield return skill;
        }
    }

    public List<SkillBase> GetSelectableSkills(int count)
    {
        if (_skillDataBase == null)
        {
            Debug.LogWarning("SkillDataBaseが未設定です");
            return new List<SkillBase>();
        }
        return _skillDataBase.GetAllSkills()
            .Where(s => !_exhaustedSkillIDs.Contains(s.ID))
            .OrderBy(_ => UnityEngine.Random.value)
            .Take(count)
            .ToList();
    }

    [SerializeField] private SkillDataBase _skillDataBase;

    private SkillExecutor _skillExecutor;
    private List<ISkillUpdater> _updaters = new();
    private HashSet<int> _ownedSkillIDs = new();
    private HashSet<int> _exhaustedSkillIDs = new();
    private HashSet<int> _registeredSkillIDs = new();
    private Dictionary<int, int> _skillAcquireCounts = new();

    private IEnumerable<SkillBase> GetSkillsByTiming(SkillTiming timing)
        => GetOwnedSkills().Where(s => s.Timing == timing);

    private void Update() => _skillExecutor?.Tick();
}
