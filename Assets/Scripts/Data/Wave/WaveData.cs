using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 1ウェーブ分のSpawnGroupリスト
/// </summary>
[CreateAssetMenu(fileName = "WaveData", menuName = "GameData/Wave Data")]
public class WaveData : ScriptableObject
{
    public List<SpawnGroupData> SpawnGroups = new();
}
