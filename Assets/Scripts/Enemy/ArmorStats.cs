using System;
using UnityEngine;

/// <summary>
/// EnemyStatsと同じ役割として作成
/// </summary>
public class ArmorStats
{
    public event Action OnBroken;

    // HP変化通知イベント (current, max)
    public event Action<float, float> OnHealthChanged;

    public float CurrentHealth => _currentHealth;

    public ArmorStats(ArmorData data)
    {
        _maxHealth = data.MaxHP;
        _currentHealth = _maxHealth;
    }

    public void TakeDamage(float damage)
    {
        _currentHealth = Mathf.Max(0, _currentHealth - damage);

        // HP変化を通知する
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

        if (_currentHealth <= 0)
        {
            Break();
        }
    }

    public void Break()
    {
        OnBroken?.Invoke();
    }

    private float _maxHealth;
    private float _currentHealth;
}
