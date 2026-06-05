using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 1Wave内の1SpawnGroupのデータクラス
/// </summary>
[System.Serializable]
public class SpawnGroupData
{
    [Tooltip("出現させるエネミーのリスト")]
    public List<WaveSpawnEnemy> SpawnEntries = new();

    [Tooltip("プレイヤーがこの距離内にいるSpawnPointを除外する半径")]
    public float ExclusionRadius = 10f;

    [Tooltip("次グループへ進む条件（OR判定: いずれか1つが発火したら進む）")]
    public List<NextWaveConditionData> NextWaveConditions = new();
}
