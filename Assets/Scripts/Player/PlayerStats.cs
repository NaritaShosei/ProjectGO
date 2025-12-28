using System;
using UnityEngine;

public class PlayerStats
{
    public event Action OnDead;

    public PlayerStats(StatsData data, PlayerStateManager state)
    {
        _data = data;
        _playerStateManager = state;

        _maxHealth = data.MaxHealth;
        _maxStamina = data.MaxStamina;

        _currentHealth = _maxHealth;
        _currentStamina = _maxStamina;
    }


    public void TakeDamage(float damage)
    {
        if (_playerStateManager.IsDead()) return;

        _currentHealth = Mathf.Max(0, _currentHealth - damage);

        if (_currentHealth <= 0)
        {
            OnDead?.Invoke();
        }
    }


    public void Heal(float amount)
    {
        if (_playerStateManager.IsDead()) { return; }

        _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
    }

    public bool UseStamina(float amount)
    {
        if (_playerStateManager.IsDead()) { return false; }

        if (_currentStamina < amount)
        {
            return false;
        }

        _currentStamina = Mathf.Max(0, _currentStamina - amount);
        return true;
    }

    public void RegenerateStamina(float regenPerSecond)
    {
        if (_playerStateManager.IsDead()) { return; }

        float regenAmountThisFrame = regenPerSecond * Time.deltaTime;
        _currentStamina = Mathf.Min(_maxStamina, _currentStamina + regenAmountThisFrame);
    }


    private StatsData _data;
    private PlayerStateManager _playerStateManager;

    private float _maxHealth;
    private float _currentHealth;

    private float _maxStamina;
    private float _currentStamina;
}
