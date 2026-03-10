using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public event Action OnEnemyDefeated;
    public event Action OnBossDefeated;
    public event Action<IEnemy> OnEnemySpawned;

    /// <summary>
    /// プレイヤー参照と各サービスを初期化する
    /// </summary>
    public void Init(IPlayer player)
    {
        if (player == null)
        {
            Debug.LogError("EnemyManager.Init: player が null です");
            enabled = false;
            return;
        }
        _player = player;

        // サービスのインスタンスを生成
        _spatialHashGrid = new SpatialHashGrid(_spatialHashGridCellSize);
        _separationService = new SeparationService(_spatialHashGrid);
        _wallAvoidanceService = new WallAvoidanceService(_wallLayerMask);
        _attackerSlot = new EnemyAttackerSlot(_maxAttackerSlots);
    }

    public void Spawn(GameObject original, Vector3 pos)
    {
        if (_player == null)
        {
            Debug.LogError("EnemyManagerが未初期化のままSpawnされました");
            return;
        }

        var obj = Instantiate(original, pos, Quaternion.identity, parent: transform);

        if (obj.TryGetComponent(out IEnemy enemy))
        {
            enemy.OnDead += HandleEnemyDead;

            // InjectServicesをInitより前に呼ぶ
            // Init内でBehaviourを生成する際にサービスを参照するため
            if (obj.TryGetComponent(out Enemy enemyBase))
            {
                enemyBase.InjectServices(
                    _spatialHashGrid,
                    _separationService,
                    _wallAvoidanceService,
                    _attackerSlot
                );
            }

            enemy.Init(_player);

            // SpatialHashGridに初期位置を登録する
            _spatialHashGrid.Register(enemy, pos);

            _enemies.Add(enemy);
            OnEnemySpawned?.Invoke(enemy);
        }
        else
        {
            Destroy(obj);
            Debug.LogWarning("IEnemyを継承していないオブジェクトを生成したため、破壊しました");
        }
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

    [Header("Spatial Hash Grid")]
    // グリッドの1辺のサイズ
    [SerializeField] private float _spatialHashGridCellSize = 2.0f;

    [Header("Wall Avoidance")]
    // 壁判定に使用するレイヤーマスク
    [SerializeField] private LayerMask _wallLayerMask;

    [Header("Attacker Slot")]
    // 同時攻撃可能な最大数
    [SerializeField] private int _maxAttackerSlots = 3;

    private List<IEnemy> _enemies = new();
    private IPlayer _player;

    private ISpatialHashGrid _spatialHashGrid;
    private ISeparationService _separationService;
    private IWallAvoidanceService _wallAvoidanceService;
    private IEnemyAttackerSlot _attackerSlot;

    private void HandleEnemyDead(IEnemy enemy)
    {
        if (enemy != null)
        {
            enemy.OnDead -= HandleEnemyDead;

            // SpatialHashGridから登録解除
            _spatialHashGrid?.Remove(enemy);

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
