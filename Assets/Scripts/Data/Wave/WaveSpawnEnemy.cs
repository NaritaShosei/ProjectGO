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

    [Min(1), Tooltip("Wave合計EXP指定時の1体あたりの配分ウェイト。3ならウェイト1の敵の約3倍。")]
    public int ExperienceWeight = 1;

    [Tooltip("中ボスの場合のみ設定。プレイヤーレベルに応じたEnemyDataの切り替えテーブル")]
    public MidBossLevelTable MidBossLevelTable;

    public bool IsMidBoss => MidBossLevelTable != null;
}
