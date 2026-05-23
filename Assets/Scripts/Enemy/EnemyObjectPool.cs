using UnityEngine;

/// <summary>
/// エネミーのオブジェクトプールクラス
/// </summary>
public class EnemyObjectPool
{
    private readonly GenericObjectPool<Enemy> _pool;

    /// <summary>
    /// Poolの初期化
    /// </summary>
    /// <param name="prefab">生成元のprefab</param>
    /// <param name="parent">生成する親</param>
    /// <param name="preloadCount">事前生成する数</param>
    public EnemyObjectPool(Enemy prefab, Transform parent, int preloadCount)
    {
        _pool = new GenericObjectPool<Enemy>(prefab, parent, preloadCount);
    }

    public Enemy Get()
    {
        Enemy enemy = _pool.Get();
        Debug.Log($"Get : {enemy.name}");
        return enemy;
    }

    public void Release(Enemy enemy)
    {
        Debug.Log($"Release : {enemy.name}");
        _pool.Release(enemy);
    }
}
