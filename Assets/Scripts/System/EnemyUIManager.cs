using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

public class EnemyUIManager : MonoBehaviour
{
    public void Init(Transform playerTransform)
    {
        _playerTransform = playerTransform;
    }

    [SerializeField] private EnemyManager _enemyManager;
    [SerializeField] private EnemyGaugeView _gaugePrefab;
    [SerializeField] private Transform _gaugeParent;
    [SerializeField] private float _detectionRange = 10f;
    [SerializeField] private float _damagedDisplayDuration = 3f;
    [SerializeField] private float _rangeCheckInterval = 0.1f; // 距離チェック間隔(秒)

    private Transform _playerTransform;

    private Dictionary<IEnemy, EnemyGaugePresenter> _presenters = new();
    private EnemyGaugePool _pool;
    private CancellationTokenSource _cts;

    private void Awake()
    {
        // Debug用のフォールバック。通常はEnemyManager.Initでセットされる想定
        _playerTransform = FindAnyObjectByType<Player>().transform;

        _pool = new EnemyGaugePool(_gaugePrefab, _gaugeParent);
        _enemyManager.OnEnemySpawned += HandleEnemySpawned;

        _cts = new CancellationTokenSource();
        RangeCheckLoopAsync(_cts.Token).Forget();
    }

    /// <summary>
    /// 一定間隔で全Presenterの距離チェックを行うループ
    /// Updateより頻度を落とすことでGC・CPU負荷を軽減
    /// </summary>
    private async UniTaskVoid RangeCheckLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            foreach (var presenter in _presenters.Values)
            {
                presenter.UpdateRangeCheck();
            }
            await UniTask.Delay(
                System.TimeSpan.FromSeconds(_rangeCheckInterval),
                cancellationToken: ct
            );
        }
    }

    private void HandleEnemySpawned(IEnemy enemy)
    {
        var view = _pool.Get();
        var presenter = new EnemyGaugePresenter(
            enemy,
            view,
            _playerTransform,
            _detectionRange,
            _damagedDisplayDuration
        );
        _presenters.Add(enemy, presenter);
        enemy.OnDead += HandleEnemyDead;
    }

    private void HandleEnemyDead(IEnemy enemy)
    {
        if (_presenters.TryGetValue(enemy, out var presenter))
        {
            presenter.ResetView();
            presenter.Dispose();
            _pool.Release(presenter.View);
            _presenters.Remove(enemy);
        }
        enemy.OnDead -= HandleEnemyDead;
    }

    private void OnDestroy()
    {
        _cts.Cancel();
        _cts.Dispose();

        _enemyManager.OnEnemySpawned -= HandleEnemySpawned;
        foreach (var pair in _presenters)
        {
            pair.Key.OnDead -= HandleEnemyDead;
            pair.Value.Dispose();
        }
        _presenters.Clear();
    }
}
public class EnemyGaugePool
{
    public EnemyGaugePool(EnemyGaugeView prefab, Transform parent)
    {
        _prefab = prefab;
        _parent = parent;
    }

    public EnemyGaugeView Get()
    {
        EnemyGaugeView view;

        if (_pool.Count > 0)
        {
            view = _pool.Pop();
            view.gameObject.SetActive(true);
        }
        else
        {
            view = Object.Instantiate(_prefab, _parent);
        }

        return view;
    }

    public void Release(EnemyGaugeView view)
    {
        view.Cleanup();
        view.gameObject.SetActive(false);
        _pool.Push(view);
    }

    private EnemyGaugeView _prefab;
    private Transform _parent;

    private Stack<EnemyGaugeView> _pool = new();
}
