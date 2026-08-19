using System.Collections.Generic;
using UnityEngine;
using static SpawnGroupData;

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

        // 出現時刻になった予約済みEnemyをスポーンする
        ProcessPendingSpawns();

        // 全Group終了後に残敵が全滅したらWave完了
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

    private int _groupSpawnCount;

    private readonly Queue<SpawnRequest> _pendingSpawns = new Queue<SpawnRequest>();
    private readonly Dictionary<EnemyGroup, int>
        _pendingGroupCounts = new();


    private readonly struct SpawnRequest
    {
        public readonly string EnemyTypeKey;
        public readonly Vector3 Position;
        public readonly float SpawnTime;
        public readonly MidBossLevelTable MidBossLevelTable;
        public readonly EnemyGroup Group;
        public readonly bool IsGroupLeader;

        // MidBossLevelTableが設定されている場合は中ボスとして扱う
        public bool IsMidBoss => MidBossLevelTable != null;

        public SpawnRequest(
            string enemyTypeKey,
            Vector3 position,
            float spawnTime,
            MidBossLevelTable midBossLevelTable,
            EnemyGroup group,
            bool isGroupLeader)
        {
            EnemyTypeKey = enemyTypeKey;
            Position = position;
            SpawnTime = spawnTime;
            MidBossLevelTable = midBossLevelTable;

            Group = group;
            IsGroupLeader = isGroupLeader;
        }
    }

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

        float now = Time.time;

        var group = _currentWave.SpawnGroups[_currentGroupIndex];

        SpawnPoint spawnPoint =
            _spawnPointSelector.Select(
                group.ExclusionRadius,
                 group.SpawnPointKey);

        if (spawnPoint == null)
        {
            Debug.LogError("SpawnPoint取得失敗");
            return false;
        }

        ScheduleSpawnGroup(group, spawnPoint, now);

        _groupStartTime = now;

        _groupKillCount = 0;
        _groupSpawnCount = GetTotalSpawnCount(group);

        return true;
    }

    /// <summary>
    /// SpawnGroup内の敵のスポーンを予約する
    /// </summary>
    /// <param name="group"></param>
    /// <param name="spawnPoint"></param>
    private void ScheduleSpawnGroup(
        SpawnGroupData group,
        SpawnPoint spawnPoint,
        float baseTime)
    {
        int setSize =
            Mathf.Max(
                1,
                group.SpawnSetSize);

        float setInterval =
            Mathf.Max(
                0f,
                group.SpawnSetInterval);

        int totalCount =
            GetTotalSpawnCount(group);

        if (totalCount <= 0)
            return;

        // Cluster配置をグループ生成として扱う
        EnemyGroup enemyGroup = null;

        if (group.PlacementMode ==
            SpawnPlacementMode.Cluster)
        {
            enemyGroup =
                new EnemyGroup(
                    group.ClusterRadius);

            _pendingGroupCounts[enemyGroup] =
                totalCount;
        }

        int flatIndex = 0;

        foreach (var entry in group.SpawnEntries)
        {
            for (int i = 0;
                 i < entry.SpawnCount;
                 i++)
            {
                int setIndex =
                    flatIndex / setSize;

                float spawnTime =
                    baseTime +
                    setIndex * setInterval;

                Vector3 position =
                    CalculateSpawnPosition(
                        group,
                        spawnPoint,
                        flatIndex,
                        totalCount);

                bool isGroupLeader =
                    enemyGroup != null &&
                    flatIndex == 0;

                _pendingSpawns.Enqueue(
                    new SpawnRequest(
                        entry.EnemyTypeKey,
                        position,
                        spawnTime,
                        entry.MidBossLevelTable,
                        enemyGroup,
                        isGroupLeader));

                flatIndex++;
            }
        }
    }

    /// <summary>
    /// 生成された敵をグループへ登録する。
    /// </summary>
    private void RegisterGroupMember(
        Enemy spawnedEnemy,
        SpawnRequest request)
    {
        if (request.Group == null)
            return;

        if (spawnedEnemy is
            IEnemyGroupMember groupMember)
        {
            bool isLeader =
                request.IsGroupLeader ||
                request.Group.Leader == null;

            request.Group.AddMember(
                spawnedEnemy,
                groupMember,
                isLeader);
        }
        else if (spawnedEnemy != null)
        {
            Debug.LogWarning(
                $"{spawnedEnemy.name}は" +
                $"{nameof(IEnemyGroupMember)}を" +
                "実装していません。");
        }

        if (!_pendingGroupCounts.TryGetValue(
                request.Group,
                out int remaining))
        {
            return;
        }

        remaining--;

        if (remaining <= 0)
        {
            _pendingGroupCounts.Remove(request.Group);

            if (request.Group.Members.Count > 0)
            {
                _enemyManager.RegisterWaitingGroup(
                    request.Group);
            }

            return;
        }

        _pendingGroupCounts[request.Group] = remaining;
    }



    /// <summary>
    /// SpawnGroup内の敵のスポーン位置を計算する
    /// </summary>
    /// <param name="group"></param>
    /// <param name="spawnPoint"></param>
    /// <param name="index"></param>
    /// <param name="totalCount"></param>
    /// <returns></returns>
    private Vector3 CalculateSpawnPosition(
    SpawnGroupData group,
    SpawnPoint spawnPoint,
    int index,
    int totalCount)
    {
        if (group.PlacementMode ==
            SpawnPlacementMode.SpawnPointSlots)
        {
            return spawnPoint.GetSlotPosition(index);
        }

        Vector3 center =
            spawnPoint.GetSlotPosition(0);

        // 1体目は中心
        if (index == 0)
            return center;

        int childCount = totalCount - 1;

        if (childCount <= 0)
            return center;

        int childIndex = index - 1;

        float angle =
            childIndex * Mathf.PI * 2f / childCount;

        Vector3 offset = new Vector3(
            Mathf.Cos(angle),
            0f,
            Mathf.Sin(angle)
        ) * group.ClusterRadius;

        return center + offset;
    }

    /// <summary>
    /// 予約済みスポーンのうち、時刻が来たものを消化する
    /// </summary>
    private void ProcessPendingSpawns()
    {
        while (_pendingSpawns.Count > 0 &&
               _pendingSpawns.Peek().SpawnTime <=
               Time.time)
        {
            SpawnRequest request =
                _pendingSpawns.Dequeue();

            if (request.IsMidBoss)
            {
                _enemyManager.SpawnMidBoss(
                    request.EnemyTypeKey,
                    request.Position,
                    request.MidBossLevelTable);

                RegisterGroupMember(null, request);
                continue;
            }

            Enemy spawnedEnemy =
                _enemyManager.Spawn(
                    request.EnemyTypeKey,
                    request.Position);

            RegisterGroupMember(
                spawnedEnemy,
                request);
        }
    }

    /// <summary>
    /// SpawnGroupの次のグループに進む条件をチェックする
    /// </summary>
    private void CheckNextGroup()
    {
        // 時間差スポーン中は次Groupへ進めない
        if (_pendingSpawns.Count > 0) return;

        var group =
            _currentWave.SpawnGroups[_currentGroupIndex];

        foreach (var condition in group.NextWaveConditions)
        {
            if (IsNextGroupConditionSatisfied(condition))
            {
                MoveToNextGroup();
                return;
            }
        }
    }

    /// <summary>
    /// 次のグループに進む条件を満たしているか判定
    /// </summary>
    /// <param name="condition"></param>
    /// <returns></returns>
    private bool IsNextGroupConditionSatisfied(
    NextWaveConditionData condition)
    {
        switch (condition.WaveConditionType)
        {
            case WaveConditionType.TimeElapsed:
                {
                    bool isTimeElapsed = Time.time - _groupStartTime
                           >= condition.Threshold;

                    return isTimeElapsed;
                }

            case WaveConditionType.KillCount:
                {
                    bool hasReachedKillCount = _groupKillCount >= condition.Threshold;

                    return hasReachedKillCount;
                }

            case WaveConditionType.AllDefeated:
                {
                    bool isAllDefeated = _groupKillCount >= _groupSpawnCount;

                    return isAllDefeated;
                }
        }

        return false;
    }

    /// <summary>
    /// 次のグループに進む
    /// </summary>
    private void MoveToNextGroup()
    {
        _currentGroupIndex++;

        if (_currentGroupIndex >= _currentWave.SpawnGroups.Count)
        {
            return;
        }

        if (!ExecuteCurrentGroup())
        {
            Debug.LogError("[WaveController] 次グループ開始失敗");
            // Wave全体を失敗として終了
            IsComplete = true;
        }
    }

    private void ResetState()
    {
        _currentWave = null;
        _currentGroupIndex = 0;
        _groupKillCount = 0;
        _groupSpawnCount = 0;
        _waveKillCount = 0;
        _groupStartTime = 0f;
        IsComplete = false;
        _pendingSpawns.Clear();
        _pendingGroupCounts.Clear();
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
