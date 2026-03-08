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

    [Header("Reference")]
    [SerializeField] private EnemyManager _enemyManager;

    [Header("Gauge")]
    [SerializeField] private EnemyGaugeView _gaugePrefab;
    [SerializeField] private Transform _gaugeParent;

    [Header("Damage Popup")]
    [SerializeField] private DamagePopupView _popupPrefab;
    [SerializeField] private Transform _popupParent;

    [Header("Settings")]
    [SerializeField] private float _detectionRange = 10f;
    [SerializeField] private float _damagedDisplayDuration = 3f;
    [SerializeField] private float _rangeCheckInterval = 0.1f;
    [SerializeField] private int _popupPreloadCount = 20;

    private Transform _playerTransform;

    private Dictionary<IEnemy, EnemyGaugePresenter> _presenters = new();

    private EnemyGaugePool _gaugePool;
    private DamagePopupPool _popupPool;
    private DamagePopupPresenter _popupPresenter;

    private CancellationTokenSource _cts;

    private void Awake()
    {
        _gaugePool = new EnemyGaugePool(_gaugePrefab, _gaugeParent);

        _popupPool = new DamagePopupPool(
            _popupPrefab,
            _popupParent,
            _popupPreloadCount
        );

        _popupPresenter = new DamagePopupPresenter(_popupPool);

        _enemyManager.OnEnemySpawned += HandleEnemySpawned;

        _cts = new CancellationTokenSource();
        RangeCheckLoopAsync(_cts.Token).Forget();
    }

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
        // Gauge
        var view = _gaugePool.Get();

        var presenter = new EnemyGaugePresenter(
            enemy,
            view,
            _playerTransform,
            _detectionRange,
            _damagedDisplayDuration
        );

        _presenters.Add(enemy, presenter);

        // Damage Popup
        // enemy.OnDamageDealt += HandleDamageDealt;
        enemy.OnDead += HandleEnemyDead;
    }

    private void HandleDamageDealt(DamagePopupViewModel viewModel)
    {
        _popupPresenter.Show(viewModel);
    }

    private void HandleEnemyDead(IEnemy enemy)
    {
        if (_presenters.TryGetValue(enemy, out var presenter))
        {
            presenter.ResetView();
            presenter.Dispose();

            _gaugePool.Release(presenter.View);

            _presenters.Remove(enemy);
        }

        // enemy.OnDamageDealt -= HandleDamageDealt;
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
            // pair.Key.OnDamageDealt -= HandleDamageDealt;

            pair.Value.Dispose();
        }

        _presenters.Clear();

        _popupPresenter.Dispose();
    }
}
