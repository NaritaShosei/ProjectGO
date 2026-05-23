using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private List<EnemyPoolData> _enemyData;
    [SerializeField] private Transform _enemyParent;
    [SerializeField] private int _preloadCount = 10;

    private readonly Dictionary<string, EnemyObjectPool> _pools = new();

    private void Awake()
    {
        foreach (EnemyPoolData data in _enemyData)
        {
            if (string.IsNullOrEmpty(data.Key))
            {
                Debug.LogWarning("Enemy Key is Empty");
                continue;
            }

            if (data.Prefab == null)
            {
                Debug.LogWarning($"Prefab Missing : {data.Key}");
                continue;
            }

            if (_pools.ContainsKey(data.Key))
            {
                Debug.LogWarning($"Duplicate Key : {data.Key}");
                continue;
            }

            EnemyObjectPool pool =
                new EnemyObjectPool(
                    data.Prefab,
                    _enemyParent,
                    _preloadCount);

            _pools.Add(data.Key, pool);
        }
    }

    public Enemy Spawn(string poolKey, Vector3 position)
    {
        Debug.Log($"Pool Count : {_pools.Count}");
        if (!_pools.TryGetValue(poolKey, out EnemyObjectPool pool))
        {
            Debug.LogWarning($"Enemy not found: {poolKey}");
            return null;
        }

        Enemy enemy = pool.Get();

        enemy.SetPoolKey(poolKey);

        enemy.transform.position = position;

        enemy.OnDead += HandleEnemyDeath;
        return enemy;
    }

    private void HandleEnemyDeath(IEnemy enemy)
    {
        if (enemy is not Enemy e)
        {
            Debug.LogWarning("Enemy cast failed");
            return;
        }
        enemy.OnDead -= HandleEnemyDeath;

        if (_pools.TryGetValue(e.PoolKey, out var pool))
        {
            pool.Release(e);
        }
        else
        {
            Debug.LogError($"Pool not found : {e.PoolKey}");
        }
    }
}
