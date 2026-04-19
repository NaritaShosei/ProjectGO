using System;
using UnityEngine;

public class SkillExecutor
{
    public SkillExecutor(SkillManager skillManager, IPlayerStats stats, IModeController modeController, Transform playerTransform, EnemyManager enemyManager)
    {
        _skillManager = skillManager;
        _playerStats = stats;
        this._modeController = modeController;
        _enemyManager = enemyManager;
        _playerTransform = playerTransform;
    }

    public void Tick()
    {
        if (_skillManager == null) return;

        float deltatime = Time.deltaTime;

        foreach (var updater in _skillManager.GetUpdaters())
        {
            updater.OnUpdate(deltatime, _modeController.CurrentMode, _playerStats, _playerTransform.position, _enemyManager);
        }
    }

    private SkillManager _skillManager;
    private IPlayerStats _playerStats;
    private IModeController _modeController;
    private Transform _playerTransform;
    private EnemyManager _enemyManager;
}
