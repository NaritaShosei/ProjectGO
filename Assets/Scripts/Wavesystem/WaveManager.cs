using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Wave全体の管理をするクラス
/// </summary>
public class WaveManager : MonoBehaviour
{
    [SerializeField] private WaveSequenceData _waveSequenceData;
    [SerializeField] private EnemySpawner _enemySpawner;
    [SerializeField] private SpawnPointSelector _spawnPointSelector;

    //現在のキルカウント(Wave開始時にリセット)
    private int _waveKillCount;

    private int _currentGroupIndex;
    private int _currentWaveIndex;

    private int _currentGroupSpawnedCount;
    private int _currentGroupDefeatedCount;

    // 遅延実行中のCoroutineを追跡（終了判定に使用）
    private Coroutine _activeGroupCoroutine;
    private bool _isGroupCoroutineRunning;


    public void Init()
    {
        _spawnPointSelector.Initialize();
        StartWave(_currentWaveIndex);
    }

    private void StartWave(int waveIndex)
    {
        if (waveIndex >= _waveSequenceData.Waves.Count)
        {
            Debug.Log("[WaveManager] 全ウェーブ終了");
            return;
        }

        _waveKillCount = 0;
        _currentGroupIndex = 0;

        Debug.Log($"[WaveManager] Wave {waveIndex + 1} 開始");
        ExecuteGroup(_waveSequenceData.Waves[waveIndex], _currentGroupIndex);
    }

    private void ExecuteGroup(WaveData waveData, int groupIndex)
    {
        if (groupIndex >= waveData.SpawnGroups.Count)
        {
            // 全Group実行済み → シークエンス終了を監視へ
            StartCoroutine(WaitForSequenceEnd());
            return;
        }

        SpawnGroupData group = waveData.SpawnGroups[groupIndex];

        _currentGroupSpawnedCount = 0;
        _currentGroupDefeatedCount = 0;

        // SpawnPoint選択
        int totalSpawnCount = CalcTotalSpawnCount(group);
        SpawnPoint spawnPoint = _spawnPointSelector.Select(group.ExclusionRadius, totalSpawnCount);

        if (spawnPoint == null)
        {
            Debug.LogWarning($"[WaveManager] Group {groupIndex}: SpawnPointが見つかりませんでした。スキップします");
            AdvanceToNextGroup();
            return;
        }

        // エネミー生成
        List<Vector3> slots = spawnPoint.GetWorldSlotPositions();
        int slotIndex = 0;

        foreach (var entry in group.SpawnEntries)
        {
            for (int i = 0; i < entry.SpawnCount; i++)
            {
                Enemy enemy = _enemySpawner.Spawn(entry.EnemyTypeKey, slots[slotIndex]);
                if (enemy != null)
                {
                    enemy.OnReleaseRequested += _ => OnEnemyDefeated();
                    _currentGroupSpawnedCount++;
                }
                slotIndex++;
            }
        }

        // NextWaveCondition監視開始
        _isGroupCoroutineRunning = true;
        _activeGroupCoroutine = StartCoroutine(
            WatchNextWaveConditions(group, _currentGroupSpawnedCount));
    }

    // ----------------------------------------------------------------
    // NextWaveCondition監視
    // ----------------------------------------------------------------

    private IEnumerator WatchNextWaveConditions(SpawnGroupData group, int spawnedCount)
    {
        float elapsed = 0f;

        while (true)
        {
            elapsed += Time.deltaTime;

            foreach (var condition in group.NextWaveConditions)
            {
                if (EvaluateCondition(condition, elapsed, spawnedCount))
                {
                    _isGroupCoroutineRunning = false;
                    AdvanceToNextGroup();
                    yield break;
                }
            }

            yield return null;
        }
    }

    private bool EvaluateCondition(
        NextWaveConditionData condition, float elapsed, int spawnedCount)
    {
        return condition.WaveConditionType switch
        {
            WaveConditionType.TimeElapsed => elapsed >= condition.Threshold,
            WaveConditionType.KillCount => _waveKillCount >= (int)condition.Threshold,
            WaveConditionType.AllDefeated => _currentGroupDefeatedCount >= spawnedCount,
            _ => false
        };
    }

    // ----------------------------------------------------------------
    // エネミー撃破通知
    // ----------------------------------------------------------------

    private void OnEnemyDefeated()
    {
        _waveKillCount++;
        _currentGroupDefeatedCount++;
    }

    // ----------------------------------------------------------------
    // Group・Wave進行
    // ----------------------------------------------------------------

    private void AdvanceToNextGroup()
    {
        _currentGroupIndex++;
        ExecuteGroup(_waveSequenceData.Waves[_currentWaveIndex], _currentGroupIndex);
    }

    /// <summary>
    /// シークエンス終了条件を全て満たすまで待機する
    /// 条件：出現中エネミー数が0 / 未実行Group無し / 遅延待機中Group無し
    /// </summary>
    private IEnumerator WaitForSequenceEnd()
    {
        yield return new WaitUntil(() =>
            _enemySpawner.ActiveEnemyCount == 0 &&
            _currentGroupIndex >= _waveSequenceData.Waves[_currentWaveIndex].SpawnGroups.Count &&
            !_isGroupCoroutineRunning);

        Debug.Log($"[WaveManager] Wave {_currentWaveIndex + 1} 終了");
        _currentWaveIndex++;
        StartWave(_currentWaveIndex);
    }

    // ----------------------------------------------------------------
    // ユーティリティ
    // ----------------------------------------------------------------

    /// <summary>
    /// SpawnGroupの総SpawnCountを算出する
    /// </summary>
    private int CalcTotalSpawnCount(SpawnGroupData group)
    {
        int total = 0;
        foreach (var entry in group.SpawnEntries)
            total += entry.SpawnCount;
        return total;
    }
}
