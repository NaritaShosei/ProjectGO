using System;
using System.Collections.Generic;
using UnityEngine;

using BossEnemy.Character;
using BossEnemy.Infrastructure;
using BossEnemy.Interface;
using BossEnemy.UI;

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
    public IBossEnemyCharacterView Spawn(Vector3 position, out IBossHPView bossEnemyHPUI)
    {
        BossCharacterView enemyView = _bossEnemyObjectPool.Get();
        bossEnemyHPUI = _enemyUIObjectPool.Get();

        enemyView.InjectServices(_services);
        enemyView.SetPosition(position);
        enemyView.SetSpawner(_attackHitAreaSpawner);

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

    private EnemyServices _services;
    private GenericObjectPool<BossCharacterView> _bossEnemyObjectPool;
    private GenericObjectPool<BossEnemyHPView> _enemyUIObjectPool;

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
