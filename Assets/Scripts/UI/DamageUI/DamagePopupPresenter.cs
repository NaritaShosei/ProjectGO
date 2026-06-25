using UnityEngine;
using System;
public class DamagePopupPresenter : IDisposable
{
    public DamagePopupPresenter(GenericObjectPool<DamagePopupView> pool)
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

    private readonly GenericObjectPool<DamagePopupView> _pool;

    private void HandleRelease(IDamagePopupView view)
    {
        view.OnRelease -= HandleRelease;
        _pool.Release(view as DamagePopupView);
    }
}

public readonly struct DamagePopupViewModel
{
    public readonly int Damage;
    public readonly bool IsWeakPoint;
    public readonly bool IsCritical;
    public readonly Vector3 WorldPosition;
    public readonly Color? TextColor;

    public DamagePopupViewModel(int damage, bool isWeakPoint, bool isCritical, Vector3 worldPosition, Color? textColor = null)
    {
        Damage = damage;
        IsWeakPoint = isWeakPoint;
        IsCritical = isCritical;
        WorldPosition = worldPosition;
        TextColor = textColor ?? DamagePopupColorScope.Current;
    }
}

public static class DamagePopupColorScope
{
    public static readonly Color LightningColor = new Color(0.25f, 0.65f, 1f);

    public static Color? Current => _current;

    // Enemy側に表示色を渡さず、TakeDamage中に生成されるポップアップだけ一時的に色を差し替える。
    public static IDisposable Use(Color color)
    {
        return new Scope(color);
    }

    private static Color? _current;

    private sealed class Scope : IDisposable
    {
        public Scope(Color color)
        {
            _previous = _current;
            _current = color;
        }

        public void Dispose()
        {
            _current = _previous;
        }

        private readonly Color? _previous;
    }
}
