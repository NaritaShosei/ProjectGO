using UnityEngine;

/// <summary>
/// 1Wave内のSpawnGroup進行を管理するクラス
/// 敵のスポーン、グループの遷移判定、Wave完了判定を担当
/// </summary>
public class WaveController
{
    public bool IsComplete { get; private set; }

    public WaveController(
        EnemyManager enemyManager,
        SpawnPointSelector spawnPointSelector)
    {
        _enemyManager = enemyManager;
        _spawnPointSelector = spawnPointSelector;
    }

    /// <summary>
    /// Waveの進行を更新する
    /// 毎フレーム呼び出されることを想定
    /// </summary>
    public void Tick()
    {
        // Wave終了後は処理しない
        if (IsComplete)
            return;

        // Wave未開始
        if (_currentWave == null)
            return;

        // 全Group終了後は残敵確認のみ行う
        if (_currentGroupIndex >= _currentWave.SpawnGroups.Count)
        {
            if (_enemyManager.GetEnemyCount() == 0)
            {
                IsComplete = true;
            }

            return;
        }

        CheckNextGroup();
    }

    /// <summary>
    /// 新しいWaveを開始する
    /// </summary>
    /// <param name="waveData"></param>
    public bool StartWave(WaveData waveData)
    {
        ResetState();

        if (waveData == null)
        {
            Debug.LogError("WaveDataがnullです");
            return false;
        }


        if (waveData.SpawnGroups == null ||
            waveData.SpawnGroups.Count == 0)
        {
            Debug.LogError("[WaveController] SpawnGroupが設定されていません");
            return false;
        }

        _currentWave = waveData;
        IsComplete = false;

        return ExecuteCurrentGroup();
    }


    /// <summary>
    /// 撃破カウントの更新
    /// </summary>
    public void OnEnemyDefeated()
    {
        _groupKillCount++;
        _waveKillCount++;
    }

    private readonly EnemyManager _enemyManager;
    private readonly SpawnPointSelector _spawnPointSelector;

    private WaveData _currentWave;

    private int _currentGroupIndex;

    private float _groupStartTime;

    private int _groupKillCount;

    private int _waveKillCount;

    /// <summary>
    /// 現在のSpawnGroupを実行する
    /// スポーンポイントの選択と敵のスポーンを行う
    /// </summary>
    private bool ExecuteCurrentGroup()
    {
        if (_currentWave == null)
        {
            Debug.LogError("[WaveController] CurrentWaveがnullです");
            return false;
        }

        if (_currentGroupIndex >= _currentWave.SpawnGroups.Count)
        {
            Debug.LogError("[WaveController] GroupIndexが範囲外です");
            return false;
        }

        var group = _currentWave.SpawnGroups[_currentGroupIndex];

        SpawnPoint spawnPoint =
            _spawnPointSelector.Select(
                group.ExclusionRadius,
                 group.SpawnPointKey);

        if (spawnPoint == null)
        {
            ResetState();
            Debug.LogError("SpawnPoint取得失敗");
            return false;
        }

        SpawnGroup(group, spawnPoint);

        _groupStartTime = Time.time;

        _groupKillCount = 0;

        return true;
    }

    /// <summary>
    /// SpawnGroup内の敵をスポーン実行する
    /// </summary>
    /// <param name="group"></param>
    /// <param name="spawnPoint"></param>
    private void SpawnGroup(
    SpawnGroupData group,
    SpawnPoint spawnPoint)
    {
        int slotIndex = 0;

        foreach (var entry in group.SpawnEntries)
        {
            for (int i = 0; i < entry.SpawnCount; i++)
            {
                Vector3 position =
                    spawnPoint.GetSlotPosition(slotIndex);

                _enemyManager.Spawn(
                    entry.EnemyTypeKey,
                    position);

                slotIndex++;
            }
        }
    }

    /// <summary>
    /// SpawnGroupの次のグループに進む条件をチェックする
    /// </summary>
    private void CheckNextGroup()
    {
        var group =
            _currentWave.SpawnGroups[_currentGroupIndex];

        foreach (var condition in group.NextWaveConditions)
        {
            if (IsConditionSatisfied(condition))
            {
                MoveNextGroup();
                return;
            }
        }
    }

    /// <summary>
    /// 次のグループに進む条件を満たしているか判定
    /// </summary>
    /// <param name="condition"></param>
    /// <returns></returns>
    private bool IsConditionSatisfied(
    NextWaveConditionData condition)
    {
        switch (condition.WaveConditionType)
        {
            case WaveConditionType.TimeElapsed:

                return Time.time - _groupStartTime
                       >= condition.Threshold;

            case WaveConditionType.KillCount:

                return _waveKillCount
                       >= condition.Threshold;

            case WaveConditionType.AllDefeated:

                return _enemyManager.GetEnemyCount() == 0;
        }

        return false;
    }

    /// <summary>
    /// 次のグループに進む
    /// </summary>
    private void MoveNextGroup()
    {
        _currentGroupIndex++;

        if (_currentGroupIndex >= _currentWave.SpawnGroups.Count)
        {
            return;
        }

        if (!ExecuteCurrentGroup())
        {
            Debug.LogError("[WaveController] 次グループ開始失敗");
        }
    }

    private void ResetState()
    {
        _currentWave = null;
        _currentGroupIndex = 0;
        _groupKillCount = 0;
        _waveKillCount = 0;
        IsComplete = true;
    }

    /// <summary>
    /// SpawnGroup内の敵の総数を計算する
    /// </summary>
    /// <param name="group"></param>
    /// <returns></returns>
    private int GetTotalSpawnCount(
    SpawnGroupData group)
    {
        int count = 0;

        foreach (var entry in group.SpawnEntries)
        {
            count += entry.SpawnCount;
        }

        return count;
    }
}
