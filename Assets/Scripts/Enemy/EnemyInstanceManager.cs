using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyInstanceManager : MonoBehaviour
{
    [SerializeField] SpeedManager _speedManager;
    private readonly List<EnemyBase> _enemiesOnField = new List<EnemyBase>();
    private readonly List<ISpeedChange> _speedChangeOnField = new List<ISpeedChange>();
    /// <summary>
    /// フィールド上に敵が一体もいなければ true
    /// </summary>
    public bool AllEnemiesDefeated => _enemiesOnField.Count == 0;

    /// <summary>
    /// 生成時の親（ヒエラルキー整理用）。必要なら Inspector で指定。
    /// </summary>
    [SerializeField] private Transform _spawnParent;
    private float _timeScale = 1f;

    private void Start()
    {
        if (_speedManager != null)
        {
            _speedManager.OnSpeedChanged += SpeedChange;
        }
    }
    private void OnDestroy()
    {
        if (_speedManager != null)
        {
            _speedManager.OnSpeedChanged -= SpeedChange;
        }
    }
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
        var e = PoolManager.Instance.Spawn(enemyPrefab);
        if (e == null)
        {
            Debug.LogError("敵の生成に失敗しました。");
            return;
        }
        e.Init(playerTransform, this);
        e.transform.SetPositionAndRotation(pos, rot);
        RegisterSpeed(e);//eはEnemyBaseでISpeedChangeを継承している
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
    public void RegisterSpeed(ISpeedChange speedChange)
    {
        if (speedChange == null) return;
        _speedChangeOnField.Add(speedChange);
        speedChange.OnSpeedChange(_timeScale);
    }
    public void UnRegisterSpeed(ISpeedChange speedChange)
    {
        if (speedChange == null) return;
        _speedChangeOnField.Remove(speedChange);
    }
    public void SpeedChange(float scale)
    {
        Debug.Log($"全体のTimeScaleを{scale}に");
        _timeScale = scale;

        // 無効な参照をスキップ
        for (int i = _speedChangeOnField.Count - 1; i >= 0; i--)
        {
            var s = _speedChangeOnField[i];
            if (s == null)
            {
                _speedChangeOnField.RemoveAt(i);
                continue;
            }
            s.OnSpeedChange(scale);
        }
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
