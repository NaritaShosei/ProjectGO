using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using System.Linq;

public class ItemPickupManager : MonoBehaviour, IItemInteractHandler
{
    public void Init(Transform playerTransform)
    {
        _playerTransform = playerTransform;
        _itemPool = new HealItemPool(_itemPrefab, _itemParent);
        _viewPool = new ItemPickupPool(_viewPrefab, _viewParent);

        _cts = new CancellationTokenSource();
        RangeCheckLoopAsync(_cts.Token).Forget();
    }

    public void Spawn(Vector3 position)
    {
        var item = _itemPool.Get(position);
        var view = _viewPool.Get();
        var presenter = new ItemPickupPresenter(
            item, view, _playerTransform, _nearRange, _interactRange
        );
        presenter.OnPickedUp += HandlePickedUp;
        _presenters[item] = presenter;
    }

    // --- IItemInteractHandler ---

    public void SetNearTarget(IInteractable interactable)
    {
        // 近づいたことをPresenterに知らせる必要はない
        // OverlapSphereの結果はUpdateRangeCheckが担うため何もしない
        // 必要であれば将来的にハイライト演出などをここで行う
        // 実際にインタラクト可能なものをハイライト表示したい場合はここでPresenterに知らせる必要があるかもしれない
    }

    public void ClearNearTarget(IInteractable interactable)
    {
        // 同上
    }

    public void OnPlayerInteract(GameObject interactor)
    {
        // Interact状態のPresenterを探して実行する
        // プレイヤーとアイテムの距離で近い順に処理
        foreach (var presenter in _presenters.Values.OrderBy(p => Vector3.Distance(p.Item.transform.position, _playerTransform.position)))
        {
            if (presenter.View.CurrentState != ItemPickupViewState.Interact) continue;
            presenter.Item.Interact(interactor);
            return; // 1フレームで1アイテムのみ
        }
    }

    [Header("HealItem")]
    [SerializeField] private HealItem _itemPrefab;
    [SerializeField] private Transform _itemParent;

    [Header("View")]
    [SerializeField] private ItemPickupView _viewPrefab;
    [SerializeField] private Transform _viewParent;

    [Header("距離設定")]
    [SerializeField] private float _nearRange = 5f;
    [SerializeField] private float _interactRange = 2f;
    [SerializeField] private float _rangeCheckInterval = 0.1f;

    private Transform _playerTransform;
    private HealItemPool _itemPool;
    private ItemPickupPool _viewPool;
    private CancellationTokenSource _cts;

    // アイテムをキーにすることでOnPlayerInteractから素早く引ける
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
