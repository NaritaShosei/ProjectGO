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
    float nearRange)
    {
        Item = item;
        View = view;
        _playerTransform = playerTransform;
        _nearRangeSq = nearRange * nearRange;

        View.Initialize(item.transform); // Hidden状態にリセット済み
        item.OnInteracted += HandleItemInteracted;

        UpdateRangeCheck();
    }

    public void UpdateRangeCheck()
    {
        if (_playerTransform == null || Item == null) return;

        float sqrDist = (_playerTransform.position - Item.transform.position).sqrMagnitude;
        bool isNear = sqrDist <= _nearRangeSq;

        // Interact状態はSetInteractTargetが管理するため上書きしない
        // ただしNear圏外に出たらInteractも解除する
        if (View.CurrentState == ItemPickupViewState.Interact)
        {
            if (!isNear) View.SetState(ItemPickupViewState.Hidden);
        }
        else
        {
            View.SetState(isNear ? ItemPickupViewState.Near : ItemPickupViewState.Hidden);
        }
    }

    // Managerから「インタラクト対象に選ばれた/外れた」を通知される
    public void SetInteractTarget(bool isTarget)
    {
        if (isTarget)
            View.SetState(ItemPickupViewState.Interact);
        else if (View.CurrentState == ItemPickupViewState.Interact)
            View.SetState(ItemPickupViewState.Near);
    }

    public void Dispose()
    {
        Item.OnInteracted -= HandleItemInteracted;
        OnPickedUp = null;
    }

    private readonly Transform _playerTransform;
    private readonly float _nearRangeSq;

    private void HandleItemInteracted(HealItem _) => OnPickedUp?.Invoke(this);
}
