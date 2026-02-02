using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public event Action OnEnemyDefeated;
    public event Action OnBossDefeated;

    public void Init(IPlayer player)
    {
        _player = player;

        if (player == null)
        {
            Debug.LogError("EnemyManager.Init: player が null です");
            enabled = false;
            return;
        }
        _player = player;
    }

    public void Spawn(GameObject original, Vector3 pos)
    {
        if (_player == null)
        {
            Debug.LogError("EnemyManagerが未初期化のままSpawnされました");
            return;
        }

        // TODO: 親をEnemyManagerにしているので、Goblinがうまく移動できていない
        var obj = Instantiate(original, pos, Quaternion.identity, parent: transform);

        if (obj.TryGetComponent(out IEnemy enemy))
        {
            enemy.OnDead += HandleEnemyDead;
            enemy.Init(_player);
            _enemies.Add(enemy);
        }

        else { Destroy(obj); Debug.LogWarning("IEnemyを継承していないオブジェクトを生成したため、破壊しました"); }
    }
    public int GetEnemyCount() => _enemies.Count;

    /// <summary>
    /// SpawnDataRepositoryから一括生成
    /// </summary>
    public void SpawnFromRepository(SpawnDataRepository repository)
    {
        if (repository == null || repository.SpawnDatas == null) return;

        foreach (var spawnData in repository.SpawnDatas)
        {
            var strategy = spawnData.CreateStrategy(this);
            strategy.Spawn();
        }
    }

    /// <summary>
    /// ボスを生成
    /// </summary>
    public void SpawnBoss(GameObject bossPrefab, Vector3 position)
    {
        Spawn(bossPrefab, position);
    }

    private List<IEnemy> _enemies = new();
    private IPlayer _player;

    private void HandleEnemyDead(IEnemy enemy)
    {
        if (enemy != null)
        {
            enemy.OnDead -= HandleEnemyDead;
            _enemies.Remove(enemy);

            // ボスかどうか判定
            if (enemy is BossEnemy)
            {
                OnBossDefeated?.Invoke();
            }
            else
            {
                OnEnemyDefeated?.Invoke();
            }
        }
    }

    // デバッグ用
    private void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 200, 30), $"残り敵数：{_enemies.Count}");
    }
}
