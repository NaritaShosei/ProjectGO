using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyInstanceManager : MonoBehaviour
{
    private readonly List<EnemyBase> _enemiesOnField = new List<EnemyBase>();
    [SerializeField]ObjectPoolManager _objectPoolManager;
    /// <summary>
    /// フィールド上に敵が一体もいなければ true
    /// </summary>
    public bool AllEnemiesDefeated => _enemiesOnField.Count == 0;

    /// <summary>
    /// 生成時の親（ヒエラルキー整理用）。必要なら Inspector で指定。
    /// </summary>
    [SerializeField] private Transform _spawnParent;
    /// <summary>
    /// 外部から敵を登録したい場合に使う。重複登録は無視される。
    /// </summary>
    public void RegisterEnemy(EnemyBase enemy)
    {
        if (enemy == null) return;

        _enemiesOnField.Add(enemy);
    }

    /// <summary>
    /// Prefab を与えて生成するユーティリティ。
    /// enemyPrefab はプレハブを想定。spawnPos が null の場合は
    /// manager の位置を使う。
    /// </summary>
    public void Spawn(EnemyBase enemyPrefab, Transform spawnPos, Transform playerTransform)
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning("Spawn called with null enemyPrefab.");
            return;
        }

        Vector3 pos = (spawnPos != null) ? spawnPos.position : transform.position;
        Quaternion rot = (spawnPos != null) ? spawnPos.rotation : Quaternion.identity;

        // 親を指定して生成（null なら 自分自身）
        var parent = _spawnParent != null ? _spawnParent : this.transform;
        var poolobj = _objectPoolManager.Get(enemyPrefab.gameObject);
        var e = poolobj as EnemyBase;
        e.Init(playerTransform);
        e.transform.SetPositionAndRotation(pos, rot);

        // 登録
        _enemiesOnField.Add(e);

        // イベント解除可能な形で登録する（クロージャで handler を保持しておく）
        Action handler = null;
        handler = () =>
        {
            // 登録解除
            _enemiesOnField.Remove(e);

            // 念のためイベントハンドラも解除しておく
            e.OnDeath -= handler;
        };

        e.OnDeath += handler;
    }

    /// <summary>
    /// 外部から個別に解除したいときに使う。
    /// </summary>
    public void UnRegisterEnemy(EnemyBase enemy)
    {
        if (enemy == null) return;
        _enemiesOnField.Remove(enemy);
    }

    [ContextMenu("EnemyCount")]
    public int GetEnemyCount()
    {
        return _enemiesOnField.Count;
    }

    // 全敵を強制的にクリア
    [ContextMenu("Kill All")]
    public void ForceClearAllEnemies()
    {
        while (_enemiesOnField.Count > 0)
        {
            var enemy = _enemiesOnField[0];
            enemy.ForceKill();
        }

        _enemiesOnField.Clear();
    }
}
