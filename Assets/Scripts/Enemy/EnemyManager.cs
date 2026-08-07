using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

using BossEnemy.View;

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

        // 中ボス生成時にプレイヤーレベルを参照するためEXPManagerを取得
        if (!ServiceLocator.TryGet(out _expManager))
        {
            Debug.LogError("EnemyManager.Init: EXPManager が ServiceLocator に未登録です");
        }


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

            // FormationSystemへの登録はInit前に行う
            // Init内のTryAcquireが呼ばれる時点でIsVanguardが確定している必要があるため
            if (_formationSystem != null && obj.TryGetComponent(out IFormationParticipant participant))
            {
                _formationSystem.Register(enemy, participant);
            }

            OnEnemySpawned?.Invoke(enemy);

            enemy.Init();

            if (enemy is Enemy enemyComponent)
            {
                enemyComponent.ReInitialize(pos);
                enemyComponent.PlaySpawnAnimation();
                enemyComponent.OnRegisteredToFormation();
            }

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
    /// 通常Enemy生成用のオーバーロード。
    /// EnemyDataを上書きせずデフォルトデータで生成する。
    /// </summary>
    public void Spawn(string poolKey, Vector3 pos) => Spawn(poolKey, pos, null);

    /// <summary>
    /// オブジェクトプールを使用したエネミーの生成
    /// </summary>
    /// <param name="poolKey">取得するEnemyのPool識別キー</param>
    /// <param name="pos">出現座標</param>
    /// <param name="overrideData">上書きするEnemyData（nullなら通常のSpawnと同じ挙動）</param>
    public void Spawn(string poolKey, Vector3 pos, EnemyData overrideData)
    {
        if (_player == null)
        {
            Debug.LogError("EnemyManagerが未初期化のままSpawnされました");
            return;
        }

        Enemy enemy = _enemySpawner.Spawn(poolKey, pos, overrideData);
        if (enemy == null) return;

        // Enemy死亡時と被弾時のイベント登録
        enemy.OnDead += HandleEnemyDead;
        enemy.OnDamaged += HandleEnemyDamaged;

        _enemiesTransformList.Add(enemy.transform);

        if (_formationSystem != null && enemy.TryGetComponent(out IFormationParticipant participant))
        {
            _formationSystem.Register(enemy, participant);
        }

        enemy.OnRegisteredToFormation();

        OnEnemySpawned?.Invoke(enemy);

        _spatialHashGrid.Register(enemy, enemy.Self.position);
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

    /// <summary>
    /// プレイヤーレベルに応じたEnemyDataを選択して中ボスを生成する
    /// </summary>
    /// <param name="poolKey"></param>
    /// <param name="pos"></param>
    /// <param name="midBossLevelTable"></param>
    public void SpawnMidBoss(string poolKey, Vector3 pos, MidBossLevelTable midBossLevelTable)
    {
        if (_player == null)
        {
            Debug.LogError("EnemyManagerが未初期化のままSpawnされました");
            return;
        }

        if (midBossLevelTable == null)
        {
            Debug.LogError($"MidBossLevelTable が未設定です（poolKey: {poolKey}）");
            return;
        }

        if (_expManager == null)
        {
            Debug.LogError($"EXPManager が未登録のため中ボスを生成できません（poolKey: {poolKey}）");
            return;
        }

        // プレイヤーレベルに応じたEnemyDataを選択
        int playerLevel = _expManager.CurrentLevel;
        EnemyData enemyData = MidBossLevelSystem.SelectEnemyData(midBossLevelTable, playerLevel);

        if (enemyData == null)
        {
            Debug.LogError($"中ボスのEnemyData選択に失敗しました（poolKey: {poolKey}）");
            return;
        }

        // 選択したEnemyDataで中ボスを生成
        Spawn(poolKey, pos, enemyData);
    }

    /// <summary> ボスを生成 </summary>
    public void SpawnBoss(string poolKey, Vector3 pos)
    {
        if (_player == null)
        {
            Debug.LogError("EnemyManagerが未初期化のままSpawnされました");
            return;
        }

        BossEnemyView enemy =　_bossEnemySpawner.Spawn(pos, out BossEnemyUIView bossEnemyUIView);
        if (enemy == null) return;

        // Enemy死亡時と被弾時のイベント登録
        enemy.OnDead += HandleEnemyDead;
        enemy.OnDamaged += HandleEnemyDamaged;
        enemy.OnChangeLockOnParts += HandleChangeBossEnemyLockOnParts;

        _enemiesTransformList.Add(enemy.Self);

        OnEnemySpawned?.Invoke(enemy);

        enemy.Init();

        _spatialHashGrid.Register(enemy, pos);
        _enemies.Add(enemy);
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

    private EXPManager _expManager;

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

            if (enemy is not BossEnemyView bossEnemy) return;

            bossEnemy.OnChangeLockOnParts -= HandleChangeBossEnemyLockOnParts;
            HandleChangeBossEnemyLockOnParts(null, bossEnemy.ActiveBossEnemyPartsView);
        }
    }

    private void HandleChangeBossEnemyLockOnParts(IReadOnlyList<ILockOnTarget> newTargets, IReadOnlyList<ILockOnTarget> oldTargets)
    {
        if (oldTargets != null)
        {
            foreach (var target in oldTargets)
            {
                if (_lockOnTargets.Contains(target)) _lockOnTargets.Remove(target);
            }
        }

        if (newTargets != null)
        {
            foreach (var target in newTargets)
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
