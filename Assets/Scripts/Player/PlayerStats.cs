using System;
using UnityEngine;

public class PlayerStats
{
    public event Action OnDead;

    public PlayerStats(StatsData data)
    {
        _maxHealth = data.MaxHealth;
        _maxStamina = data.MaxStamina;

        _currentHealth = _maxHealth;
        _currentStamina = _maxStamina;
    }

    public void TakeDamage(float damage)
    {
        _currentHealth = Mathf.Max(0, _currentHealth - damage);

        if (_currentHealth <= 0)
        {
            OnDead?.Invoke();
        }
    }

    public void Heal(float amount)
    {
        _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
    }

    public bool UseStamina(float amount)
    {
        if (_currentStamina < amount)
        {
            return false;
        }

        _currentStamina = Mathf.Max(0, _currentStamina - amount);
        return true;
    }

    public void RegenerateStamina(float regenPerSecond)
    {
        float regenAmountThisFrame = regenPerSecond * Time.deltaTime;
        _currentStamina = Mathf.Min(_maxStamina, _currentStamina + regenAmountThisFrame);
    }

    private float _maxHealth;
    private float _currentHealth;

    private float _maxStamina;
    private float _currentStamina;
}
