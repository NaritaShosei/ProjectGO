using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SkillManager : MonoBehaviour
{
    public event Action<SkillBase> OnSkillAcquired;
    public event Action<StatSkillType> OnApply;
    public StatUpgradeState StatUpgrades { get; } = new StatUpgradeState();

    public void Init(IPlayerStats stats, IModeController modeController,
        Transform playerTransform, EnemyManager enemyManager)
    {
        if (_skillDataBase == null)
        {
            Debug.LogError("[SkillManager] SkillDataBase is missing.", this);
            return;
        }

        foreach (var skill in _skillDataBase.GetAllSkills())
        {
            if (skill.DefaultUnlocked)
                _unlockedSkillIDs.Add(skill.ID);
        }

        if (ServiceLocator.TryGet(out EXPManager eXPManager))
        {
            if (_statSkillSystem != null)
            {
                _statSkillSystem.OnApply -= HandleStatApplied;
                _statSkillSystem.Dispose();
            }
            StatUpgrades.ResetCounts();
            _statSkillSystem = new StatSkillSystem(_statSkillDataArray, stats, eXPManager);
            _statSkillSystem.OnApply += HandleStatApplied;
        }
        else
        {
            Debug.LogWarning("[SkillManager] EXPManager is missing. Stat skills are disabled.", this);
        }

        _skillExecutor = new SkillExecutor(this, stats, modeController, playerTransform, enemyManager);

        // 最初から所持させるスキルの付与
        _acquisitionVoiceCount = 0;
        _pendingAcquisitionVoice = null;
        GrantInitialSkills(stats);
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

    public void QueueAcquisitionVoice()
    {
        // 初期所持スキルやステータス強化は数えず、選択で獲得した順番に対応する。
        if (_acquisitionVoiceCount >= _acquisitionVoiceCues.Length) return;
        _pendingAcquisitionVoice = _acquisitionVoiceCues[_acquisitionVoiceCount];
        _acquisitionVoiceCount++;
    }

    public string TakeAcquisitionVoice()
    {
        string cueName = _pendingAcquisitionVoice;
        _pendingAcquisitionVoice = null;
        return cueName;
    }
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
            if (_ownedSkillIDs.Contains(entry.RequiredSkillId))
            {
                if (entry.Level > max) max = entry.Level;
            }
        }
        return max;
    }

    [SerializeField] private SkillDataBase _skillDataBase;
    [SerializeField] private StatSkillData[] _statSkillDataArray;
    [SerializeField] private ChargeLevelSkillEntry[] _chargeLevelSkillMap;

    [Header("最初から所持させるスキル")]
    [Tooltip("ここに設定したスキルはInit時に自動で獲得済み扱いになる。\n" +
            "※対象のSkillBaseアセットは_skillDataBaseにも登録しておくこと（IDで検索するため）。\n" +
            "※OnAcquireを確実に発火させるため、対象スキルのTimingは「獲得時」(OnAcquire)に設定すること。")]
    [SerializeField] private SkillBase[] _initialSkills;

    private SkillExecutor _skillExecutor;
    private int _acquisitionVoiceCount;
    private string _pendingAcquisitionVoice;
    private readonly string[] _acquisitionVoiceCues =
    {
        SoundCueNames.PlayerVoice.SkillGet01,
        SoundCueNames.PlayerVoice.SkillGet02,
        SoundCueNames.PlayerVoice.SkillGet03,
        SoundCueNames.PlayerVoice.SkillGet04,
        SoundCueNames.PlayerVoice.SkillGet05,
        SoundCueNames.PlayerVoice.SkillGet06
    };
    private StatSkillSystem _statSkillSystem;
    private List<ISkillUpdater> _updaters = new();
    private HashSet<int> _ownedSkillIDs = new(); // 獲得したスキルIDのセット。重複なしで管理。
    private HashSet<int> _exhaustedSkillIDs = new(); // 既に選択肢に出たことのあるスキルIDのセット。これも重複なしで管理。
    private HashSet<int> _registeredSkillIDs = new(); // パッシブスキルのIDセット。これも重複なしで管理。
    private HashSet<int> _unlockedSkillIDs = new(); // 解放されたスキルIDのセット。これも重複なしで管理。

    private IEnumerable<SkillBase> GetSkillsByTiming(SkillTiming timing)
        => GetOwnedSkills().Where(s => s.Timing == timing);

    private void Update() => _skillExecutor?.Tick();

    private void OnDestroy()
    {
        if (_statSkillSystem != null)
            _statSkillSystem.OnApply -= HandleStatApplied;
        _statSkillSystem?.Dispose();
    }

    private void HandleStatApplied(StatSkillType type)
    {
        // 通知前に回数を確定し、UIの接続前や非表示中の取得も復元可能にする。
        StatUpgrades.AddAcquisition(type);
        OnApply?.Invoke(type);
    }

    /// <summary>
    /// _initialSkillsに設定されたスキルを、通常の獲得処理(TryRegisterSkillId)と
    /// 同じ経路で付与する。OnAcquire発火・Passive登録・進化先解放も通常通り行われる。
    /// </summary>
    private void GrantInitialSkills(IPlayerStats stats)
    {
        if (_initialSkills == null) return;

        foreach (var skill in _initialSkills)
        {
            if (skill == null) continue;

            if (_ownedSkillIDs.Contains(skill.ID)) continue; // 重複付与防止

            bool registered = TryRegisterSkillId(skill.ID, stats);

            if (!registered)
            {
                Debug.LogWarning(
                    $"[SkillManager] InitialSkill '{skill.name}'(ID:{skill.ID}) の登録に失敗しました。" +
                    "SkillDataBaseに同じスキルが登録されているか確認してください。", this);
            }
        }
    }
}

[Serializable]
public struct ChargeLevelSkillEntry
{
    public PlayerMode Mode;
    public ChargeLevel Level;
    public int RequiredSkillId;
}
