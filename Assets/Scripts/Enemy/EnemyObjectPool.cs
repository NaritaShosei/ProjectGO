using UnityEngine;

public class EnemyObjectPool
{
    private readonly GenericObjectPool<Enemy> _pool;

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
