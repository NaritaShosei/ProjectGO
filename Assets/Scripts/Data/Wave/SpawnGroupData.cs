using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 1Wave内の1SpawnGroupのデータクラス
/// </summary>
[System.Serializable]
public class SpawnGroupData
{
    public enum SpawnPlacementMode
    {
        SpawnPointSlots,
        Cluster
    }

    [Tooltip("出現させるエネミーのリスト")]
    public List<WaveSpawnEnemy> SpawnEntries = new();

    [Tooltip("プレイヤーがこの距離内にいるSpawnPointを除外する半径")]
    public float ExclusionRadius = 10f;

    [Tooltip("次グループへ進む条件（OR判定: いずれか1つが発火したら進む）")]
    public List<NextWaveConditionData> NextWaveConditions = new();

    [Tooltip("使用するSpawnPointのKey。空文字の場合は自動選択")]
    public string SpawnPointKey = "";

    [Tooltip("何体ずつ同時出現させるか")]
    [SerializeField, Min(1)]
    private int _spawnSetSize = 3;

    [Tooltip("出現間隔（秒）")]
    [SerializeField, Min(0f)]
    private float _spawnSetInterval = 0.5f;

    [Header("スポーン配置")]
    [SerializeField]
    private SpawnPlacementMode _placementMode =
    SpawnPlacementMode.SpawnPointSlots;

    [SerializeField, Min(0f)]
    private float _clusterRadius = 2f;

    public SpawnPlacementMode PlacementMode =>
        _placementMode;

    public float ClusterRadius =>
        _clusterRadius;

    public int SpawnSetSize => _spawnSetSize;
    public float SpawnSetInterval => _spawnSetInterval;
}
