using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

public class EnemyUIManager : MonoBehaviour
{
    public void Init(EnemyManager enemyManager, Transform playerTransform)
    {
        _enemyManager = enemyManager;
        _playerTransform = playerTransform;

        _gaugePool = new GenericObjectPool<EnemyGaugeView>(_gaugePrefab, _gaugeParent, 0,
            onRelease: view => view.Cleanup());

        _armerGaugePool = new GenericObjectPool<EnemyGaugeView>(_armerGaugePrefab, _armerGaugeParent, 0,
            onRelease: view => view.Cleanup());

        _popupPool = new GenericObjectPool<DamagePopupView>(_popupPrefab, _popupParent, _popupPreloadCount);

        _popupPresenter = new DamagePopupPresenter(_popupPool);

        _enemyManager.OnEnemySpawned += HandleEnemySpawned;

        _cts = new CancellationTokenSource();
        RangeCheckLoopAsync(_cts.Token).Forget();
    }


    [Header("Gauge")]
    [SerializeField] private EnemyGaugeView _gaugePrefab;
    [SerializeField] private EnemyGaugeView _armerGaugePrefab;
    [SerializeField] private Transform _gaugeParent;
    [SerializeField] private Transform _armerGaugeParent;

    [Header("Damage Popup")]
    [SerializeField] private DamagePopupView _popupPrefab;
    [SerializeField] private Transform _popupParent;

    [Header("Settings")]
    [SerializeField] private float _detectionRange = 10f;
    [SerializeField] private float _damagedDisplayDuration = 3f;
    [SerializeField] private float _rangeCheckInterval = 0.1f;
    [SerializeField] private int _popupPreloadCount = 20;

    private EnemyManager _enemyManager;
    private Transform _playerTransform;

    private Dictionary<IEnemy, EnemyGaugePresenter> _gaugePresenters = new();
    private Dictionary<IArmorHealth, ArmorGaugePresenter> _armorPresenters = new();

    private GenericObjectPool<EnemyGaugeView> _gaugePool;
    private GenericObjectPool<EnemyGaugeView> _armerGaugePool;
    private GenericObjectPool<DamagePopupView> _popupPool;
    private DamagePopupPresenter _popupPresenter;

    private CancellationTokenSource _cts;

    private async UniTaskVoid RangeCheckLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            foreach (var presenter in _gaugePresenters.Values)
            {
                presenter.UpdateRangeCheck();
            }

            foreach (var presenter in _armorPresenters.Values)
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

        _gaugePresenters.Add(enemy, presenter);

        // Damage Popup
        enemy.OnDamageDealt += HandleDamageDealt;

        if (enemy is MobEnemy mob)
        {
            mob.OnArmorRegistered += HandleArmorRegistered;
        }

        enemy.OnDead += HandleEnemyDead;
    }

    private void HandleDamageDealt(DamagePopupViewModel viewModel)
    {
        _popupPresenter.Show(viewModel);
    }

    private void HandleEnemyDead(IEnemy enemy)
    {
        if (_gaugePresenters.TryGetValue(enemy, out var presenter))
        {
            presenter.ResetView();
            presenter.Dispose();

            _gaugePool.Release(presenter.View);

            _gaugePresenters.Remove(enemy);
        }

        enemy.OnDamageDealt -= HandleDamageDealt;
        enemy.OnDead -= HandleEnemyDead;

        if (enemy is MobEnemy mob)
        {
            mob.OnArmorRegistered -= HandleArmorRegistered;
        }
    }

    private void HandleArmorRegistered(IArmorHealth armor)
    {
        var view = _armerGaugePool.Get();
        var presenter = new ArmorGaugePresenter(
            armor, view, _playerTransform, _detectionRange, _damagedDisplayDuration
        );
        _armorPresenters.Add(armor, presenter);

        presenter.OnBroken += HandleArmorBroken;
    }

    private void HandleArmorBroken(ArmorGaugePresenter presenter)
    {
        presenter.OnBroken -= HandleArmorBroken;
        presenter.ResetView();
        _armerGaugePool.Release(presenter.View);

        // Dictionaryから削除
        foreach (var pair in _armorPresenters)
        {
            if (pair.Value == presenter)
            {
                _armorPresenters.Remove(pair.Key);
                break;
            }
        }

        presenter.Dispose();
    }

    private void OnDestroy()
    {
        _cts.Cancel();
        _cts.Dispose();

        _enemyManager.OnEnemySpawned -= HandleEnemySpawned;

        foreach (var pair in _gaugePresenters)
        {
            pair.Key.OnDead -= HandleEnemyDead;
            pair.Key.OnDamageDealt -= HandleDamageDealt;
            pair.Value.Dispose();
        }
        _gaugePresenters.Clear();

        foreach (var pair in _armorPresenters)
        {
            pair.Value.Dispose();
        }
        _armorPresenters.Clear();

        _popupPresenter.Dispose();
    }
}
