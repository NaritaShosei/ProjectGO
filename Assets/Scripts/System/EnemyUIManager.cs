using System.Collections.Generic;
using UnityEngine;

public class EnemyUIManager : MonoBehaviour
{
    [SerializeField] private EnemyManager _enemyManager;
    [SerializeField] private EnemyGaugeView _gaugePrefab;
    [SerializeField] private Transform _gaugeParent;

    private Dictionary<IEnemy, EnemyGaugePresenter> _presenters = new();

    private EnemyGaugePool _pool;

    private void Awake()
    {
        _pool = new EnemyGaugePool(_gaugePrefab, _gaugeParent);
        _enemyManager.OnEnemySpawned += HandleEnemySpawned;
    }

    private void HandleEnemySpawned(IEnemy enemy)
    {
        var view = _pool.Get();

        var presenter = new EnemyGaugePresenter(enemy, view);

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
        _enemyManager.OnEnemySpawned -= HandleEnemySpawned;

        foreach (var presenter in _presenters.Values)
        {
            presenter.Dispose();
        }

        _presenters.Clear();
    }
}

public class EnemyGaugePool
{
    private EnemyGaugeView _prefab;
    private Transform _parent;

    private Stack<EnemyGaugeView> _pool = new();

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
        view.gameObject.SetActive(false);
        _pool.Push(view);
    }
}
