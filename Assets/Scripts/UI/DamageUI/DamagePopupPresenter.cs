using UnityEngine;
using System;
public class DamagePopupPresenter : IDisposable
{
    public DamagePopupPresenter(DamagePopupPool pool)
    {
        _pool = pool;
    }

    public void Show(DamagePopupViewModel viewModel)
    {
        var view = _pool.Get();
        view.OnRelease += HandleRelease;
        view.ShowDamage(viewModel);
    }

    public void Dispose()
    {
        // 特に何もしない、Poolごと破棄される想定
    }

    private readonly DamagePopupPool _pool;

    private void HandleRelease(IDamagePopupView view)
    {
        view.OnRelease -= HandleRelease;
        _pool.Release(view);
    }
}

public readonly struct DamagePopupViewModel
{
    public readonly int Damage;
    public readonly bool IsWeakPoint;
    public readonly bool IsCritical;
    public readonly Vector3 WorldPosition;

    public DamagePopupViewModel(int damage, bool isWeakPoint, bool isCritical, Vector3 worldPosition)
    {
        Damage = damage;
        IsWeakPoint = isWeakPoint;
        IsCritical = isCritical;
        WorldPosition = worldPosition;
    }
}
