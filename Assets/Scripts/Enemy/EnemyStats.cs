using System;
using UnityEngine;

public class EnemyStats
{
    public event Action OnDead;
    public event Action OnHealthZero;
    public event Action<float, float> OnHealthChanged;

    public EnemyStats(EnemyData data)
    {
        _maxHealth = data.MaxHP;
        _currentHealth = _maxHealth;
    }

    public void TakeDamage(float damage)
    {
        _currentHealth = Mathf.Max(0, _currentHealth - damage);
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

        if (_currentHealth <= 0)
        {
            OnHealthZero?.Invoke();
        }
    }

    public void Kill()
    {
        OnDead?.Invoke();
    }

    public void ResetHP(float maxHP)
    {
        _maxHealth = maxHP;
        _currentHealth = _maxHealth;
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
    }

    private float _maxHealth;
    private float _currentHealth;
}
