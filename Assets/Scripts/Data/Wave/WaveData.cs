using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 1ウェーブ分のSpawnGroupリスト
/// </summary>
[CreateAssetMenu(fileName = "WaveData", menuName = "GameData/Wave Data")]
public class WaveData : ScriptableObject
{
    [Tooltip("有効にすると、Wave合計EXPを各敵の配分ウェイトに応じて分配します。")]
    public bool OverrideExperience;

    [Min(0), Tooltip("全敵を倒してオーブを全回収したときの合計EXP。オーブ数ではありません。")]
    public int TotalExperience;

    public int GetEnemyCount()
    {
        int count = 0;
        foreach (var group in SpawnGroups)
            foreach (var entry in group.SpawnEntries)
                count += Mathf.Max(0, entry.SpawnCount);
        return count;
    }

    public int? GetExperienceForEnemy(int index)
    {
        if (!OverrideExperience) return null;
        int count = GetEnemyCount();
        int total = Mathf.Max(0, TotalExperience);
        if (count == 0 || index < 0 || index >= count) return 0;
        decimal totalWeight = 0;
        foreach (var group in SpawnGroups)
            foreach (var entry in group.SpawnEntries)
                totalWeight += (decimal)Mathf.Max(0, entry.SpawnCount) * Mathf.Max(1, entry.ExperienceWeight);

        decimal precedingWeight = 0;
        foreach (var group in SpawnGroups)
        foreach (var entry in group.SpawnEntries)
        {
            int entryCount = Mathf.Max(0, entry.SpawnCount);
            int weight = Mathf.Max(1, entry.ExperienceWeight);
            if (index < entryCount)
            {
                decimal start = precedingWeight + (decimal)index * weight;
                // 累積値の差分で整数化し、端数があってもWave合計を保存する。
                return (int)(decimal.Floor(total * (start + weight) / totalWeight) -
                    decimal.Floor(total * start / totalWeight));
            }
            index -= entryCount;
            precedingWeight += (decimal)entryCount * weight;
        }
        return 0;
    }

    public List<SpawnGroupData> SpawnGroups = new();
}
