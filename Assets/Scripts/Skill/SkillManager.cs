using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SkillManager : MonoBehaviour
{
    public event Action<SkillBase> OnSkillAcquired;
    public event Action<StatSkillType> OnApply
    {
        add => _statSkillSystem.OnApply += value;
        remove => _statSkillSystem.OnApply -= value;
    }

    public void Init(IPlayerStats stats, IModeController modeController,
        Transform playerTransform, EnemyManager enemyManager)
    {
        foreach (var skill in _skillDataBase.GetAllSkills())
        {
            if (skill.DefaultUnlocked)
                _unlockedSkillIDs.Add(skill.ID);
        }

        if (ServiceLocator.TryGet(out EXPManager eXPManager))
        {
            _statSkillSystem = new StatSkillSystem(_statSkillDataArray, stats, eXPManager);
        }

        _skillExecutor = new SkillExecutor(this, stats, modeController, playerTransform, enemyManager);
    }

    public bool TryRegisterSkillId(int id, IPlayerStats stats)
    {
        if (_skillDataBase == null) return false;

        var skill = _skillDataBase.GetSkill(id);

        if (skill == null) return false;

        // OnAcquire
        if (skill.Timing == SkillTiming.OnAcquire)
            skill.OnAcquire(stats);

        OnSkillAcquired?.Invoke(skill);

        _ownedSkillIDs.Add(id);

        _exhaustedSkillIDs.Add(id);

        // Passive登録
        if (skill.Timing == SkillTiming.Passive &&
            _registeredSkillIDs.Add(id))
        {
            var updater = skill.CreateUpdater();

            if (updater != null)
                _updaters.Add(updater);
        }

        // 進化先解放
        if (skill.UnlockedSkill != null)
        {
            _unlockedSkillIDs.Add(skill.UnlockedSkill.ID);
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
            .Where(s => _unlockedSkillIDs.Contains(s.ID))
            .Where(s => !_exhaustedSkillIDs.Contains(s.ID))
            .OrderBy(_ => UnityEngine.Random.value)
            .Take(count)
            .ToList();
    }

    /// <summary>
    /// 現在解放済みの最大チャージ段階を返す。
    /// マップにエントリがなければデフォルトLv1（チャージ攻撃は必ず存在する前提）。
    /// </summary>
    public ChargeLevel GetMaxChargeLevel(PlayerMode mode)
    {
        ChargeLevel max = ChargeLevel.Level1;

        if (_chargeLevelSkillMap == null) return max;

        foreach (var entry in _chargeLevelSkillMap)
        {
            if (entry.Mode != mode) continue;
            if (_unlockedSkillIDs.Contains(entry.RequiredSkillId))
            {
                if (entry.Level > max) max = entry.Level;
            }
        }
        return max;
    }

    [SerializeField] private SkillDataBase _skillDataBase;
    [SerializeField] private StatSkillData[] _statSkillDataArray;
    [SerializeField] private ChargeLevelSkillEntry[] _chargeLevelSkillMap;

    private SkillExecutor _skillExecutor;
    private StatSkillSystem _statSkillSystem;
    private List<ISkillUpdater> _updaters = new();
    private HashSet<int> _ownedSkillIDs = new();
    private HashSet<int> _exhaustedSkillIDs = new();
    private HashSet<int> _registeredSkillIDs = new();
    private HashSet<int> _unlockedSkillIDs = new();

    private IEnumerable<SkillBase> GetSkillsByTiming(SkillTiming timing)
        => GetOwnedSkills().Where(s => s.Timing == timing);

    private void Update() => _skillExecutor?.Tick();

    private void OnDestroy()
    {
        _statSkillSystem?.Dispose();
    }
}

[Serializable]
public struct ChargeLevelSkillEntry
{
    public PlayerMode Mode;
    public ChargeLevel Level;
    public int RequiredSkillId;
}
