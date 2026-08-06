using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Enemyの生成と管理を行うクラス
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    /// <summary>
    /// EnemySpawner初期化
    /// </summary>
    /// <param name="services"></param>
    public void Init(EnemyServices services)
    {
        _services = services;

        if (_enemyData == null || _enemyData.Count == 0)
        {
            Debug.LogError("EnemySpawner.Init: _enemyData が未設定です");
            return;
        }

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
    /// <param name="overrideData">上書きするEnemyData（nullなら上書きしない）</param>
    /// <returns>生成されたEnemy</returns>
    public Enemy Spawn(string poolKey, Vector3 position, EnemyData overrideData)
    {
        if (!_pools.TryGetValue(poolKey, out EnemyObjectPool pool))
        {
            Debug.LogWarning($"Enemy not found: {poolKey}");
            return null;
        }

        Enemy enemy = pool.Get();

        //返却時に使用するため、PoolKeyを保存
        enemy.SetPoolKey(poolKey);
        enemy.InjectServices(_services);

        // 使用するEnemyDataを設定する。
        // Init()より前に設定し、初期化時に正しいEnemyDataを参照できるようにする。
        enemy.SetData(overrideData);

        // Init()は内部でガード済み。初回生成時のみ初期化されるため、
        // プール再利用時はここで何もしないため毎回呼んでよい
        enemy.Init();
        enemy.ReInitialize(position);
        enemy.PlaySpawnAnimation();

        enemy.OnReleaseRequested += HandleEnemyDeath;
        return enemy;
    }

    /// <summary> Enemyの破棄 </summary>
    public void Despawn(Enemy enemy)
    {
        if (enemy == null)
        {
            Debug.LogWarning("Enemy is null");
            return;
        }
        enemy.OnReleaseRequested -= HandleEnemyDeath;
        if (_pools.TryGetValue(enemy.PoolKey, out EnemyObjectPool pool))
        {
            pool.Release(enemy);
        }
        else
        {
            Debug.LogError($"Pool not found : {enemy.PoolKey}");
        }
    }

    //プール生成するためのエネミーの一覧データ
    [SerializeField] private List<EnemyPoolData> _enemyData;
    [SerializeField] private Transform _enemyParent;
    [SerializeField] private int _preloadCount = 10;

    private EnemyServices _services;

    /// <summary>
    /// Enemyプールの辞書
    /// Key：Enemyの識別子
    /// Value:EnemyobjectPool
    /// </summary>
    private readonly Dictionary<string, EnemyObjectPool> _pools = new();

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

        e.OnReleaseRequested -= HandleEnemyDeath;

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
