using BossEnemy.Interface;
using BossEnemy.UI;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public event Action OnEnemyDefeated;
    public event Action OnBossDefeated;
    public event Action<IEnemy> OnEnemySpawned;
    public event Action<IEnemy> OnEnemyForceRemoved;

    public ReadOnlyCollection<Transform> EnemiesTransformList => _enemiesTransformList.AsReadOnly();

    /// <summary> プレイヤー参照と各サービスを初期化する </summary>
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
        _formationSystem = new EnemyFormationSystem();
        _playerInformationService = new PlayerInformationService(_player, this);

        _enemyServices = new EnemyServices(
        _spatialHashGrid,
        _separationService,
        _wallAvoidanceService,
        _formationSystem,
        _playerInformationService
        );

        if (_enemySpawner == null)
        {
            Debug.LogError("EnemyManager.Init: _enemySpawner が未設定です");
            enabled = false;
            return;
        }
        _enemySpawner.Init(_enemyServices);
        _bossEnemySpawner.Init(_enemyServices);
    }

    /// <summary> エネミーの生成 </summary>
    /// <param name="original"> 出現させたいエネミー </param>
    /// <param name="pos"> 出現させる場所 </param>
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
            enemy.OnDamaged += HandleEnemyDamaged;

            _enemiesTransformList.Add(obj.transform);

            // InjectServicesをInitより前に呼ぶ
            // Init内でBehaviourを生成する際にサービスを参照するため
            enemy.InjectServices(new EnemyServices(
                _spatialHashGrid,
                _separationService,
                _wallAvoidanceService,
                _formationSystem,
                _playerInformationService
            ));

            // Instantiateによる直接生成はReInitializeを通らないため、ここで壁との重なりを解消する。
            // 補正後の座標は下のSpatialHashGrid登録にも使用される。
            if (enemy is Enemy movableEnemy)
                movableEnemy.ResolveSpawnPosition();

            // FormationSystemへの登録はInit前に行う
            // Init内のTryAcquireが呼ばれる時点でIsVanguardが確定している必要があるため
            if (_formationSystem != null && obj.TryGetComponent(out IFormationParticipant participant))
            {
                _formationSystem.Register(enemy, participant);
            }

            OnEnemySpawned?.Invoke(enemy);

            // SpatialHashGridに初期位置を登録する
            // 指定されたposではなく、壁から押し戻された後の実座標を登録する。
            _spatialHashGrid.Register(enemy, enemy.Self.position);

            _enemies.Add(enemy);
            _lockOnTargets.Add(enemy);
        }
        else
        {
            Destroy(obj);
            Debug.LogWarning("IEnemyを継承していないオブジェクトを生成したため、破壊しました");
        }
    }

    /// <summary>
    /// オブジェクトプールを使用したエネミーの生成
    /// </summary>
    /// <param name="poolKey">取得するEnemyのPool識別キー</param>
    /// <param name="pos">出現座標</param>
    public void Spawn(string poolKey, Vector3 pos)
    {
        if (_player == null)
        {
            Debug.LogError("EnemyManagerが未初期化のままSpawnされました");
            return;
        }

        Enemy enemy = _enemySpawner.Spawn(poolKey, pos);
        if (enemy == null) return;

        // Enemy死亡時と被弾時のイベント登録
        enemy.OnDead += HandleEnemyDead;
        enemy.OnDamaged += HandleEnemyDamaged;

        _enemiesTransformList.Add(enemy.transform);

        if (_formationSystem != null && enemy.TryGetComponent(out IFormationParticipant participant))
        {
            _formationSystem.Register(enemy, participant);
        }

        OnEnemySpawned?.Invoke(enemy);

        _spatialHashGrid.Register(enemy, pos);
        _enemies.Add(enemy);
        _lockOnTargets.Add(enemy);
    }


    /// <summary>現在生存しているEnemyの数を返す</summary>
    public int GetEnemyCount() => _enemies.Count;

    public IReadOnlyList<IEnemy> GetEnemiesInRange(Vector3 position, float radius)
    {
        List<IEnemy> enemiesInRange = new List<IEnemy>();
        foreach (var enemy in _enemies)
        {
            if (enemy.IsDead) continue;
            float distance = Vector3.Distance(enemy.Self.position, position);
            if (distance <= radius)
            {
                enemiesInRange.Add(enemy);
            }
        }
        return enemiesInRange;
    }

    public IReadOnlyList<ILockOnTarget> GetLockOnTarget(Vector3 position, float radius)
    {
        List<ILockOnTarget> targets = new List<ILockOnTarget>();
        foreach (var target in _lockOnTargets)
        {
            if (target == null || !target.IsLockable) continue;

            Transform center = target.GetTargetCenter();
            if (center == null) continue;

            float distance = Vector3.Distance(center.position, position);
            if (distance <= radius)
            {
                targets.Add(target);
            }
        }
        return targets;
    }

    /// <summary> SpawnDataRepositoryから一括生成 </summary>
    public void SpawnFromRepository(SpawnDataRepository repository)
    {
        if (repository == null || repository.SpawnDatas == null) return;

        foreach (var spawnData in repository.SpawnDatas)
        {
            var strategy = spawnData.CreateStrategy(this);
            strategy.Spawn();
        }
    }

    /// <summary> ボスを生成 </summary>
    public async UniTask SpawnBoss(string poolKey, Vector3 pos)
    {
        if (_player == null)
        {
            Debug.LogError("EnemyManagerが未初期化のままSpawnされました");
            return;
        }

        BossEnemyHPView bossEnemyUIView = null;
        IBossEnemyCharacterView enemy = await _bossEnemySpawner.Spawn(pos, bossEnemyUIView);
        if (enemy == null) return;

        // Enemy死亡時と被弾時のイベント登録
        enemy.OnDead += HandleEnemyDead;
        enemy.OnDamaged += HandleEnemyDamaged;
        enemy.OnChangeLockOnParts += HandleChangeBossEnemyLockOnParts;

        _enemiesTransformList.Add(enemy.Self);

        OnEnemySpawned?.Invoke(enemy);

        _spatialHashGrid.Register(enemy, pos);
        _enemies.Add(enemy);

        enemy.StartAction();
    }

    /// <summary> スポーン中のモブ敵をプールに返して非有効化する </summary>
    public void ClearAllMobEnemies()
    {
        foreach (var enemy in _enemies.ToArray())
        {
            if (enemy != null && !enemy.IsBoss && !enemy.IsDead)
            {
                RemoveDeadEnemyTransform(enemy);

                enemy.OnDead -= HandleEnemyDead;
                enemy.OnDamaged -= HandleEnemyDamaged;

                // SpatialHashGridから登録解除
                _spatialHashGrid?.Remove(enemy);

                _enemies.Remove(enemy);

                OnEnemyForceRemoved?.Invoke(enemy);

                // モブのみを対象にするため、Enemyクラスのインスタンスかどうかを確認
                if (enemy is Enemy enemyComponent)
                {
                    _enemySpawner.Despawn(enemyComponent);
                }
            }
        }
    }

    [Header("Spatial Hash Grid")]
    // グリッドの1辺のサイズ
    [SerializeField] private float _spatialHashGridCellSize = 2.0f;

    [Header("Wall Avoidance")]
    // 壁判定に使用するレイヤーマスク
    [SerializeField] private LayerMask _wallLayerMask;

    // Enemyの生成を行うクラス
    [SerializeField] private EnemySpawner _enemySpawner;
    [SerializeField] private BossEnemySpawner _bossEnemySpawner;

    private List<Transform> _enemiesTransformList = new List<Transform>();
    private List<IEnemy> _enemies = new();
    private List<ILockOnTarget> _lockOnTargets = new List<ILockOnTarget>();
    private IPlayer _player;

    private ISpatialHashGrid _spatialHashGrid;
    private ISeparationService _separationService;
    private IWallAvoidanceService _wallAvoidanceService;
    private IEnemyFormationSystem _formationSystem;
    private IPlayerInformationService _playerInformationService;

    // Enemyに提供するサービスをまとめたクラス
    private EnemyServices _enemyServices;

    private void Awake()
    {
        ServiceLocator.Register(this);
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<EnemyManager>();
    }

    /// <summary>
    /// EnemyのOnDamagedイベントハンドラ
    /// FormationSystemに被弾を通知する
    /// </summary>
    private void HandleEnemyDamaged(IEnemy enemy)
    {
        _formationSystem?.NotifyHit(enemy.Id);
    }

    private void HandleEnemyDead(IEnemy enemy)
    {
        if (enemy != null)
        {
            RemoveDeadEnemyTransform(enemy);

            enemy.OnDead -= HandleEnemyDead;
            enemy.OnDamaged -= HandleEnemyDamaged;

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

            if (enemy is not IBossEnemyCharacterView bossEnemy) return;

            bossEnemy.OnChangeLockOnParts -= HandleChangeBossEnemyLockOnParts;
            HandleChangeBossEnemyLockOnParts((null, bossEnemy.ActiveBossEnemyPartsView));
        }
    }

    private void HandleChangeBossEnemyLockOnParts((IReadOnlyList<ILockOnTarget> newTargets, IReadOnlyList<ILockOnTarget> oldTargets) changedLockOnTargets)
    {
        if (changedLockOnTargets.oldTargets != null)
        {
            foreach (var target in changedLockOnTargets.oldTargets)
            {
                if (_lockOnTargets.Contains(target)) _lockOnTargets.Remove(target);
            }
        }

        if (changedLockOnTargets.newTargets != null)
        {
            foreach (var target in changedLockOnTargets.newTargets)
            {
                if (target != null && !_lockOnTargets.Contains(target)) _lockOnTargets.Add(target);
            }
        }
    }


    /// <summary> 死んだ敵の登録されているTransformをリムーブする </summary>
    /// <param name="enemy"> 死んだ敵 </param>
    private void RemoveDeadEnemyTransform(IEnemy enemy)
    {
        var enemyComponent = enemy as Component;

        if (enemyComponent != null)
        {
            GameObject targetEnemy = enemyComponent.gameObject;
            _enemiesTransformList.Remove(targetEnemy.transform);
            _lockOnTargets.Remove(enemy);
        }
        else
        {
            Debug.LogError("このインターフェースの実体はUnityのComponentではありません。");
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
