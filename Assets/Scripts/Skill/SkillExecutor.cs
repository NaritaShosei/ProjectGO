using System;
using UnityEngine;

public class SkillExecutor : MonoBehaviour
{
    public void Init(SkillManager skillManager,IPlayerStats stats,Func<PlayerMode> getmode,Transform playerTransform,EnemyManager enemyManager)
    {
        _skillManager = skillManager;
        _playerStats = stats;
        _getMode = getmode;
        _enemyManager = enemyManager;
        _playerTransform = playerTransform;
    }

    private SkillManager _skillManager;
    private IPlayerStats _playerStats;
    private Func<PlayerMode> _getMode;
    private Transform _playerTransform;
    private EnemyManager _enemyManager;

    void Update()
    {
        if (_skillManager == null) return;

        float deltatime = Time.deltaTime;
        var mode = _getMode?.Invoke() ?? default;

        foreach (var updater in _skillManager.GetUpdaters())
        {
            updater.OnUpdate(deltatime,mode,_playerStats,_playerTransform.position,_enemyManager);
        }
    }
}
