using UnityEngine;

/// <summary>
/// SpawnGroup内で出現させる1種類のエネミー設定
/// </summary>
[System.Serializable]
public class WaveSpawnEnemy
{
    [Tooltip("EnemySpawnRegistryDataのKeyと一致させる")]
    public string EnemyTypeKey;

    [Min(1)]
    public int SpawnCount;
}
