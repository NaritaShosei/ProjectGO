using System.Collections.Generic;
using System;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

public class EnemyUIManager : MonoBehaviour
{
    public void Init(EnemyManager enemyManager, Transform playerTransform)
    {
        _enemyManager = enemyManager;
        _playerTransform = playerTransform;

        _gaugePool = new GenericObjectPool<EnemyGaugeView>(_gaugePrefab, _gaugeParent, 0);

        _armerGaugePool = new GenericObjectPool<EnemyGaugeView>(_armerGaugePrefab, _armerGaugeParent, 0);

        _popupPool = new GenericObjectPool<DamagePopupView>(_popupPrefab, _popupParent, _popupPreloadCount);

        _popupPresenter = new DamagePopupPresenter(_popupPool);

        _enemyManager.OnEnemySpawned += HandleEnemySpawned;
        _enemyManager.OnEnemyForceRemoved += HandleEnemyDead;

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
    private Dictionary<IEnemy, List<IArmorHealth>> _enemyArmors = new();
    private Dictionary<MobEnemy, Action<IArmorHealth>> _armorRegisteredHandlers = new();

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
        Transform gaugeTarget = enemy is MobEnemy mobEnemy
            ? mobEnemy.GetUIAnchor()
            : enemy.GetTargetCenter();

        var presenter = new EnemyGaugePresenter(
            enemy,
            view,
            gaugeTarget,
            enemy.GetTargetCenter(),
            _playerTransform,
            _detectionRange,
            _damagedDisplayDuration
        );

        _gaugePresenters.Add(enemy, presenter);

        // Damage Popup
        enemy.OnDamageDealt += HandleDamageDealt;

        if (enemy is MobEnemy mob)
        {
            Action<IArmorHealth> armorRegisteredHandler = armor => HandleArmorRegistered(enemy, armor);
            _armorRegisteredHandlers.Add(mob, armorRegisteredHandler);
            mob.OnArmorRegistered += armorRegisteredHandler;

            // プール生成ではReInitialize中に鎧登録イベントが先に発火するため、
            // 購読開始時点ですでに有効な鎧があればここで同期する。
            if (mob.TryGetActiveArmor(out var armor))
            {
                HandleArmorRegistered(enemy, armor);
            }
        }

        enemy.OnDead += HandleEnemyDead;
    }

    private void HandleDamageDealt(DamagePopupViewModel viewModel)
    {
        _popupPresenter.Show(viewModel);
    }

    private void HandleEnemyDead(IEnemy enemy)
    {
        ReleaseArmorGauges(enemy);

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
            if (_armorRegisteredHandlers.TryGetValue(mob, out var armorRegisteredHandler))
            {
                mob.OnArmorRegistered -= armorRegisteredHandler;
                _armorRegisteredHandlers.Remove(mob);
            }
        }
    }

    private void HandleArmorRegistered(IEnemy enemy, IArmorHealth armor)
    {
        // イベント購読と現在値同期が同じ鎧を通知しても二重生成しない。
        if (armor == null || _armorPresenters.ContainsKey(armor)) return;

        Transform armorGaugeTarget = ResolveArmorGaugeAnchor(enemy);

        var view = _armerGaugePool.Get();
        var presenter = new ArmorGaugePresenter(
            armor,
            view,
            armorGaugeTarget,
            enemy.GetTargetCenter(),
            _playerTransform,
            _detectionRange,
            _damagedDisplayDuration
        );
        _armorPresenters.Add(armor, presenter);

        if (!_enemyArmors.TryGetValue(enemy, out var armors))
        {
            armors = new List<IArmorHealth>();
            _enemyArmors.Add(enemy, armors);
        }
        armors.Add(armor);

        presenter.OnBroken += HandleArmorBroken;
    }

    private void HandleArmorBroken(ArmorGaugePresenter presenter)
    {
        IArmorHealth brokenArmor = null;
        foreach (var pair in _armorPresenters)
        {
            if (pair.Value == presenter)
            {
                brokenArmor = pair.Key;
                break;
            }
        }

        if (brokenArmor == null) return;

        RemoveArmorOwnerLink(brokenArmor);
        ReleaseArmorGauge(brokenArmor);
    }

    private void ReleaseArmorGauges(IEnemy enemy)
    {
        if (!_enemyArmors.TryGetValue(enemy, out var armors)) return;

        foreach (var armor in armors.ToArray())
        {
            ReleaseArmorGauge(armor);
        }

        _enemyArmors.Remove(enemy);
    }

    private void ReleaseArmorGauge(IArmorHealth armor)
    {
        if (!_armorPresenters.TryGetValue(armor, out var presenter)) return;

        presenter.OnBroken -= HandleArmorBroken;
        presenter.ResetView();
        _armerGaugePool.Release(presenter.View);
        presenter.Dispose();

        _armorPresenters.Remove(armor);
    }

    private void RemoveArmorOwnerLink(IArmorHealth armor)
    {
        IEnemy owner = null;
        foreach (var pair in _enemyArmors)
        {
            if (pair.Value.Remove(armor))
            {
                owner = pair.Key;
                break;
            }
        }

        if (owner != null && _enemyArmors[owner].Count == 0)
        {
            _enemyArmors.Remove(owner);
        }
    }

    private void OnDestroy()
    {
        _cts.Cancel();
        _cts.Dispose();

        _enemyManager.OnEnemySpawned -= HandleEnemySpawned;
        _enemyManager.OnEnemyForceRemoved -= HandleEnemyDead;

        foreach (var pair in _gaugePresenters)
        {
            pair.Key.OnDead -= HandleEnemyDead;
            pair.Key.OnDamageDealt -= HandleDamageDealt;
            pair.Value.Dispose();
        }
        _gaugePresenters.Clear();

        foreach (var pair in _armorRegisteredHandlers)
        {
            pair.Key.OnArmorRegistered -= pair.Value;
        }
        _armorRegisteredHandlers.Clear();
        _enemyArmors.Clear();

        foreach (var pair in _armorPresenters)
        {
            pair.Value.OnBroken -= HandleArmorBroken;
            pair.Value.Dispose();
        }
        _armorPresenters.Clear();

        _popupPresenter.Dispose();
    }

    /// <summary>
    /// 鎧・盾ゲージの表示アンカーを決定する。
    /// ShieldDraugrは盾専用の位置、それ以外は通常のUIAnchorを使う。
    /// </summary>
    private Transform ResolveArmorGaugeAnchor(IEnemy enemy)
    {
        if (enemy is ShieldDraugr shieldDraugr)
        {
            return shieldDraugr.GetShieldGaugeAnchor();
        }

        return enemy is MobEnemy mob ? mob.GetUIAnchor() : enemy.GetTargetCenter();
    }
}
