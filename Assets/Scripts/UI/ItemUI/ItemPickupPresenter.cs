using System;
using UnityEngine;

public class ItemPickupPresenter : IDisposable
{
    public ItemPickupView View { get; }
    public HealItem Item { get; }

    public event Action<ItemPickupPresenter> OnPickedUp;

    public ItemPickupPresenter(
        HealItem item,
        ItemPickupView view,
        Transform playerTransform,
        float nearRange,
        float interactRange)
    {
        Item = item;
        View = view;
        _playerTransform = playerTransform;
        _nearRangeSq = nearRange * nearRange;
        _interactRangeSq = interactRange * interactRange;

        View.Initialize(item.transform);
        item.OnInteracted += HandleItemInteracted;
    }

    public void UpdateRangeCheck()
    {
        if (_playerTransform == null || Item == null) return;

        float sqrDist = (_playerTransform.position - Item.transform.position).sqrMagnitude;

        if (sqrDist <= _interactRangeSq)
            View.SetState(ItemPickupViewState.Interact);
        else if (sqrDist <= _nearRangeSq)
            View.SetState(ItemPickupViewState.Near);
        else
            View.SetState(ItemPickupViewState.Hidden);
    }

    public void Dispose()
    {
        Item.OnInteracted -= HandleItemInteracted;
        OnPickedUp = null;
    }

    private readonly Transform _playerTransform;
    private readonly float _nearRangeSq;
    private readonly float _interactRangeSq;

    private void HandleItemInteracted(HealItem _)
    {
        OnPickedUp?.Invoke(this);
    }
}
