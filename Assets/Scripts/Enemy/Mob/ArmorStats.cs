using System;
using UnityEngine;

/// <summary>
/// アーマーのHP管理クラス
/// ダメージ適用・HP変化通知・破壊検知を担う
/// </summary>
public sealed class ArmorStats
{
    /// <summary>アーマーが破壊されたときに発火するイベント</summary>
    public event Action OnBroken;

    /// <summary>HP変化時に発火するイベント（current, max）</summary>
    public event Action<float, float> OnHealthChanged;

    public float CurrentHealth => _currentHealth;

    public ArmorStats(ArmorData data)
    {
        _maxHealth = data.MaxHP;
        _currentHealth = _maxHealth;
    }

    /// <summary>
    /// ダメージを適用し、HPがゼロになった場合にOnBrokenを発火する
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (_isBroken) return;

        _currentHealth = Mathf.Max(0, _currentHealth - damage);
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

        if (_currentHealth <= 0) Break();
    }

    private void Break()
    {
        _isBroken = true;
        OnBroken?.Invoke();
    }

    /// <summary>
    /// HPを全回復して破壊状態を解除する
    /// </summary>
    public void RestoreFull()
    {
        _isBroken = false;
        _currentHealth = _maxHealth;

        OnHealthChanged?.Invoke(
            _currentHealth,
            _maxHealth);
    }

    private readonly float _maxHealth;
    private float _currentHealth;
    private bool _isBroken;
}
