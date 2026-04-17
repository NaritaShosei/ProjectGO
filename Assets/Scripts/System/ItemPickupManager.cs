using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

public class ItemPickupManager : MonoBehaviour, IItemInteractHandler
{
    public void Init(Transform playerTransform)
    {
        _playerTransform = playerTransform;
        _itemPool = new GenericObjectPool<HealItem>(_itemPrefab, _itemParent);
        _viewPool = new GenericObjectPool<ItemPickupView>(_viewPrefab, _viewParent);
        _cts = new CancellationTokenSource();
        RangeCheckLoopAsync(_cts.Token).Forget();
    }

    public void Spawn(Vector3 position)
    {
        var item = _itemPool.Get();
        item.transform.position = position;

        var view = _viewPool.Get();
        var presenter = new ItemPickupPresenter(
            item, view, _playerTransform, _nearRange
        );
        presenter.OnPickedUp += HandlePickedUp;
        _presenters[item] = presenter;
    }

    // --- IItemInteractHandler ---
    public void SetNearTarget(IInteractable interactable)
    {
        // 以前のInteract選択を解除
        if (_interactPresenter != null)
        {
            _interactPresenter.SetInteractTarget(false);
            _interactPresenter = null;
        }

        if (interactable is HealItem item && _presenters.TryGetValue(item, out var presenter))
        {
            presenter.SetInteractTarget(true);
            _interactPresenter = presenter;
        }
    }

    public void ClearNearTarget(IInteractable interactable)
    {
        if (interactable is HealItem item &&
            _presenters.TryGetValue(item, out var presenter) &&
            presenter == _interactPresenter)
        {
            presenter.SetInteractTarget(false);
            _interactPresenter = null;
        }
    }

    public void OnPlayerInteract(GameObject interactor)
    {
        _interactPresenter?.Item.Interact(interactor);
    }

    [Header("HealItem")]
    [SerializeField] private HealItem _itemPrefab;
    [SerializeField] private Transform _itemParent;
    [Header("View")]
    [SerializeField] private ItemPickupView _viewPrefab;
    [SerializeField] private Transform _viewParent;
    [Header("距離設定")]
    [SerializeField] private float _nearRange = 5f;
    [SerializeField] private float _rangeCheckInterval = 0.1f;

    private Transform _playerTransform;
    private GenericObjectPool<HealItem> _itemPool;
    private GenericObjectPool<ItemPickupView> _viewPool;
    private CancellationTokenSource _cts;
    private ItemPickupPresenter _interactPresenter;
    private readonly Dictionary<HealItem, ItemPickupPresenter> _presenters = new();

    private void Awake()
    {
        ServiceLocator.Register(this);
    }

    private async UniTaskVoid RangeCheckLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            foreach (var p in new List<ItemPickupPresenter>(_presenters.Values))
                p.UpdateRangeCheck();

            await UniTask.Delay(
                System.TimeSpan.FromSeconds(_rangeCheckInterval),
                cancellationToken: ct
            );
        }
    }

    private void HandlePickedUp(ItemPickupPresenter presenter)
    {
        if (_interactPresenter == presenter)
            _interactPresenter = null;

        presenter.OnPickedUp -= HandlePickedUp;
        _presenters.Remove(presenter.Item);
        _viewPool.Release(presenter.View);
        _itemPool.Release(presenter.Item);
        presenter.Dispose();
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<ItemPickupManager>();
        _cts?.Cancel();
        _cts?.Dispose();
        foreach (var p in _presenters.Values)
        {
            p.OnPickedUp -= HandlePickedUp;
            p.Dispose();
        }
        _presenters.Clear();
    }
}
