using System;
using UnityEngine;

public class PlayerStats : IAttackStats
{
    public float CurrentHealth => _currentHealth;
    public float CurrentStamina => _currentStamina;

    public float AttackPower => _attackPower;
    public float CriticalRate => _criticalRate;

    public event Action OnDead;
    public event Action<float, float> OnHealthChanged;
    public event Action<float, float> OnStaminaChanged;
    public event Action OnStatsChanged;

    public PlayerStats(PlayerData data)
    {
        // HP / スタミナ
        _maxHealth = data.Stats.MaxHealth;
        _maxStamina = data.Stats.MaxStamina;

        _currentHealth = _maxHealth;
        _currentStamina = _maxStamina;

        // 戦闘ステータス
        _attackPower = data.AttackPower;
        _criticalRate = data.CriticalRate;
    }

    public void TakeDamage(float damage)
    {
        _currentHealth = Mathf.Max(0, _currentHealth - damage);

        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

        if (_currentHealth <= 0)
        {
            OnDead?.Invoke();
        }
    }

    public void Heal(float amount)
    {
        _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);

        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
    }

    public bool UseStamina(float amount)
    {
        if (_currentStamina < amount)
        {
            return false;
        }

        _currentStamina = Mathf.Max(0, _currentStamina - amount);

        OnStaminaChanged?.Invoke(_currentStamina, _maxStamina);
        return true;
    }

    public void RegenerateStamina(float regenPerSecond)
    {
        float regenAmountThisFrame = regenPerSecond * Time.deltaTime;
        float previousStamina = _currentStamina;
        _currentStamina = Mathf.Min(_maxStamina, _currentStamina + regenAmountThisFrame);

        // float 同士をほぼ同じか比較
        // 差が大きければ回復したとみなし、イベント発火
        if (!Mathf.Approximately(previousStamina, _currentStamina))
        {
            OnStaminaChanged?.Invoke(_currentStamina, _maxStamina);
        }

    }

    public void ApplyEvolution(float attackPowerBonus, float criticalRateBonus)
    {
        _attackPower = Mathf.Max(0f, _attackPower + attackPowerBonus);
        _criticalRate += criticalRateBonus;

        OnStatsChanged?.Invoke();
    }

    private float _maxHealth;
    private float _currentHealth;

    private float _maxStamina;
    private float _currentStamina;
    private float _attackPower;
    private float _criticalRate;
}

