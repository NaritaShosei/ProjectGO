using UnityEngine;

/// <summary>
/// Enemyプールのプール生成用のデータ
/// </summary>
[System.Serializable]
public class EnemyPoolData
{
    /// <summary>
    /// Enemy 識別キー
    /// </summary>
    public string Key => _key;

    /// <summary>
    /// 生成対象の Enemy プレハブ
    /// </summary>
    public Enemy Prefab => _prefab;

    [SerializeField] private string _key;
    [SerializeField] private Enemy _prefab;
}
