using System.Collections.Generic;
using UnityEngine;

public class DamageUIManager : MonoBehaviour
{
    // TODO: EnemyManagerから敵の生成イベントを受け取って、生成された敵にダメージポップアップの表示イベントを登録する
    // TODO: 型をMonoBehaviourからIDamagePopupViewを実装したクラスに変更して、Prefabから生成するようにする
    [SerializeField] private MonoBehaviour _popupPrefab; // IDamagePopupViewを実装したPrefab
    [SerializeField] private Canvas _canvas;
    [SerializeField] private EnemyManager _enemyManager;
    [SerializeField] private int _preloadCount = 20;

    private DamagePopupPool _pool;
    private DamagePopupPresenter _presenter;

    private void Start()
    {
        _pool = new DamagePopupPool(_popupPrefab as IDamagePopupView, _canvas.transform, _preloadCount);
        _presenter = new DamagePopupPresenter(_pool);

        // _enemyManager.OnEnemySpawned += HandleEnemySpawned;
    }

    private void OnDestroy()
    {
        // _enemyManager.OnEnemySpawned -= HandleEnemySpawned;
        _presenter.Dispose();
    }

    private void HandleEnemySpawned(IEnemy enemy)
    {
        // enemy.OnDamageDealt += HandleDamageDealt;
        // enemy.OnDead += _ => enemy.OnDamageDealt -= HandleDamageDealt;
    }

    private void HandleDamageDealt(DamagePopupViewModel viewModel)
    {
        _presenter.Show(viewModel);
    }
}

public class DamagePopupPool
{
    public DamagePopupPool(IDamagePopupView prefab, Transform parent, int preloadCount)
    {
        _prefab = prefab;
        _parent = parent;

        for (int i = 0; i < preloadCount; i++)
        {
            var view = CreateView();
            _pool.Push(view);
        }
    }

    public IDamagePopupView Get()
    {
        if (_pool.Count > 0)
        {
            var view = _pool.Pop();
            return view;
        }

        return CreateView();
    }

    public void Release(IDamagePopupView view)
    {
        _pool.Push(view);
    }

    private readonly IDamagePopupView _prefab;
    private readonly Transform _parent;
    private readonly Stack<IDamagePopupView> _pool = new();

    private IDamagePopupView CreateView()
    {
        // MonoBehaviourなのでInstantiateが必要、prefabから生成
        var go = GameObject.Instantiate(_prefab as MonoBehaviour, _parent);
        return go.GetComponent<IDamagePopupView>();
    }
}
