using BossEnemy.AI.BehaviourTree;
using BossEnemy.Character;
using BossEnemy.Infrastructure;
using BossEnemy.Infrastructure.Repository;
using BossEnemy.Interface;
using BossEnemy.UI;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;

public class BossEnemySpawner : MonoBehaviour
{
    /// <summary>
    /// EnemySpawner初期化
    /// </summary>
    /// <param name="services"></param>
    public void Init(EnemyServices services)
    {
        _services = services;
        _bossEnemyObjectPool = new (_bossPoolData.BossPrefab, _enemyParent, _preloadCount);
        _enemyUIObjectPool = new(_bossPoolData.BossUIPrefab, _enemyUIParent, _preloadCount);
    }

    /// <summary>
    /// Enemyの生成する
    /// </summary>
    /// <param name="poolKey">Enemyのキー</param>
    /// <param name="position">生成位置</param>
    /// <returns>生成されたEnemy</returns>
    public async UniTask<IBossEnemyCharacterView> Spawn(Vector3 position, IBossHPView bossEnemyHPUI)
    {
        await UniTask.WaitUntil(() => _isLoadedRepositries);

        BossCharacterView enemyView = _bossEnemyObjectPool.Get();
        bossEnemyHPUI = _enemyUIObjectPool.Get();

        enemyView.InjectServices(_services);
        enemyView.SetPosition(position);
        enemyView.SetSpawner(_attackHitAreaSpawner);

        IBossCharacterEntity characterEntity = _bossCharacterEntityRepository.GetEntity(_id);

        if(!_bossAIBehaviourTreeNodeRepository.TryGetEntryNode(_id, out EntryNode entryNode))
        {
            Debug.LogError("entryNodeの取得に失敗しました");
        }

        NodeRunningConditionNotifier nodeRunningConditionNotifier = new NodeRunningConditionNotifier();
        entryNode.Init(characterEntity, nodeRunningConditionNotifier);
        enemyView.Init(characterEntity, entryNode);

        return enemyView;
    }

    [Header("プール生成するためのエネミーのデータ")]
    [SerializeField, Tooltip("BossEnemyのPrefabなどを管理しているデータクラス")] 
    private BossPoolData _bossPoolData;

    [Header("親Object")]
    [SerializeField, Tooltip("BossEnemy本体の親オブジェクト")] private Transform _enemyParent;
    [SerializeField, Tooltip("BossEnemyのUIの親オブジェクト")] private Transform _enemyUIParent;

    [Header("最初のBossEnemyの生成数")]
    [SerializeField] private int _preloadCount = 1;

    [Header("BossEnemyが使用するSpawner")]
    [SerializeField] private AttackHitAreaSpawner _attackHitAreaSpawner;

    [SerializeField, Header("スポーンさせるボスのID")]
    private int _id;

    private bool _isLoadedRepositries = false;
    private EnemyServices _services;
    private GenericObjectPool<BossCharacterView> _bossEnemyObjectPool;
    private GenericObjectPool<BossEnemyHPView> _enemyUIObjectPool;

    // 各種リポジトリクラス
    private IBossCharacterEntityRepository _bossCharacterEntityRepository;
    private IBossAIBehaviourTreeNodeRepository _bossAIBehaviourTreeNodeRepository;

    /// <summary>
    /// Enemyプールの辞書
    /// Key：Enemyの識別子
    /// Value:EnemyobjectPool
    /// </summary>
    private readonly Dictionary<string, EnemyObjectPool> _pools = new();

    private void Awake()
    {
        LoadRepositories().Forget();
    }

    private void OnDestroy()
    {
        ReleaseRepositories();
    }

    /// <summary>
    /// Enemy死亡時の処理
    /// </summary>
    /// <param name="enemy">死亡したEnemy</param>
    private void HandleEnemyDeath(IEnemy enemy)
    {
        
    }

    private async UniTask LoadRepositories()
    {
        Debug.Log("RepositryLoad開始");

        _bossCharacterEntityRepository = await AssetsLoader.LoadAssetAsync<BossCharacterEntityRepository>
            (AAGBossEnemyGroup.kAssets_Data_BossEnemy_Repositry_BossEnemyEntityRepository);

        _bossAIBehaviourTreeNodeRepository = await AssetsLoader.LoadAssetAsync<BossAIBehaviourTreeNodeRepositry>
            (AAGBossEnemyGroup.kAssets_Data_BossEnemy_Repositry_BossAIBehaviourTreeNodeRepositry);

        _bossCharacterEntityRepository.Init();

        _isLoadedRepositries = true;
        Debug.Log("RepositryLoad終了");
    }

    private void ReleaseRepositories()
    {
        _bossCharacterEntityRepository = null;
        _bossAIBehaviourTreeNodeRepository = null;

        AssetsLoader.Release(AAGBossEnemyGroup.kAssets_Data_BossEnemy_Repositry_BossEnemyEntityRepository);
        AssetsLoader.Release(AAGBossEnemyGroup.kAssets_Data_BossEnemy_Repositry_BossAIBehaviourTreeNodeRepositry);
    }

    [Serializable]
    public struct BossPoolData
    {
        public string Key => _key;
        public BossCharacterView BossPrefab => _bossPrefab;
        public BossEnemyHPView BossUIPrefab => _enemyUIPrefab;

        [Header("BossEnemyを呼び出すための名前")]
        [SerializeField] private string _key;

        [Header("BossEnemyのPrefab")]
        [SerializeField] private BossCharacterView _bossPrefab;
        [SerializeField] private BossEnemyHPView _enemyUIPrefab;
    }
}
