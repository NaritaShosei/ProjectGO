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
        _formationSystem = new EnemyFormationSystem(_maxAttackerSlots, player.GetTargetCenter(), _backAttackAngle);
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
            enemy.InjectServices(new EnemyServices(
                _spatialHashGrid,
                _separationService,
                _wallAvoidanceService,
                _formationSystem
            ));

            // FormationSystemへの登録はInit前に行う
            // Init内のTryAcquireが呼ばれる時点でIsVanguardが確定している必要があるため
            if (_formationSystem != null && obj.TryGetComponent(out IFormationParticipant participant))
            {
                _formationSystem.Register(enemy, participant);
            }

            OnEnemySpawned?.Invoke(enemy);

            enemy.Init(_player);

            // SpatialHashGridに初期位置を登録する
            _spatialHashGrid.Register(enemy, pos);

            _enemies.Add(enemy);
        }
        else
        {
            Destroy(obj);
            Debug.LogWarning("IEnemyを継承していないオブジェクトを生成したため、破壊しました");
        }
    }

    /// <summary>現在生存しているEnemyの数を返す</summary>
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

    [Header("Formation")]
    // 背後エリアとみなす角度（プレイヤー正面からの閾値）
    [SerializeField] private float _backAttackAngle = 60f;

    private List<IEnemy> _enemies = new();
    private IPlayer _player;

    private ISpatialHashGrid _spatialHashGrid;
    private ISeparationService _separationService;
    private IWallAvoidanceService _wallAvoidanceService;
    private IEnemyFormationSystem _formationSystem;

    private void Update()
    {
        // 背後移動×CoolDown によるスロット譲渡チェックを毎フレーム実行する
        _formationSystem?.Tick(Time.deltaTime);
    }

    /// <summary>
    /// EnemyのOnDeadイベントハンドラ
    /// リストから除外してEnemyDefeated / BossDefeatedを発火する
    /// </summary>
    private void HandleEnemyDead(IEnemy enemy)
    {
        if (enemy != null)
        {
            enemy.OnDead -= HandleEnemyDead;

            // SpatialHashGridから登録解除
            _spatialHashGrid?.Remove(enemy);

            _enemies.Remove(enemy);

            // ボスかどうか判定
            if (enemy.IsBoss)
            {
                OnBossDefeated?.Invoke();
            }
            else
            {
                OnEnemyDefeated?.Invoke();
            }
        }
    }

#if UNITY_EDITOR
    // デバッグ用
    private void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 200, 30), $"残り敵数：{_enemies.Count}");
    }
#endif
}
