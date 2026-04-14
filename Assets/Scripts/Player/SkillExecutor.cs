using System;
using UnityEngine;

public class SkillExecutor : MonoBehaviour
{
    private SkillManager _skillManager;
    private IPlayerStats _playerStats;
    private Func<PlayerMode> _getMode;

    public void Iint(SkillManager skillManager,IPlayerStats stats,Func<PlayerMode> getmode)
    {
        _skillManager = skillManager;
        _playerStats = stats;
        _getMode = getmode;
    }


    void Update()
    {
        if (_skillManager == null) return;

        float deltatime = Time.deltaTime;
        var mode = _getMode?.Invoke() ?? default;

        foreach (var updater in _skillManager.GetUpdaters())
        {
            updater.OnUpdate(deltatime,mode,_playerStats);
        }
    }
}
