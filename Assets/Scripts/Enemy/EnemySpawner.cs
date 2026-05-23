using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Enemyの生成と管理を行うクラス
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    //プール生成するためのエネミーの一覧データ
    [SerializeField] private List<EnemyPoolData> _enemyData;
    [SerializeField] private Transform _enemyParent;
    [SerializeField] private int _preloadCount = 10;

    /// <summary>
    /// Enemyプールの辞書
    /// Key：Enemyの識別子
    /// Value:EnemyobjectPool
    /// </summary>
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

            //Enemy用のプール生成
            EnemyObjectPool pool =
                new EnemyObjectPool(
                    data.Prefab,
                    _enemyParent,
                    _preloadCount);

            _pools.Add(data.Key, pool);
        }
    }

    /// <summary>
    /// Enemyの生成する
    /// </summary>
    /// <param name="poolKey">Enemyのキー</param>
    /// <param name="position">生成位置</param>
    /// <returns>生成されたEnemy</returns>
    public Enemy Spawn(string poolKey, Vector3 position)
    {
        if (!_pools.TryGetValue(poolKey, out EnemyObjectPool pool))
        {
            Debug.LogWarning($"Enemy not found: {poolKey}");
            return null;
        }

        Enemy enemy = pool.Get();

        //返却時に使用するため、PoolKeyを保存
        enemy.SetPoolKey(poolKey);

        enemy.transform.position = position;

        enemy.OnDead += HandleEnemyDeath;
        return enemy;
    }

    /// <summary>
    /// Enemy死亡時の処理
    /// </summary>
    /// <param name="enemy">死亡したEnemy</param>
    private void HandleEnemyDeath(IEnemy enemy)
    {
        //EnemyをEnemyクラスにキャスト
        if (enemy is not Enemy e)
        {
            Debug.LogWarning("Enemy cast failed");
            return;
        }

        enemy.OnDead -= HandleEnemyDeath;

        //PoolKeyを元にEnemyをプールに返却
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
