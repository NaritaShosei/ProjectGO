using System;
using UnityEngine;

public class PlayerStats
{
    public float InitialMaxHealth { get; private set; }
    public float MaxHealth => _maxHealth;
    public float MaxStamina => _maxStamina;

    public float InitialMaxStamina { get; private set; }
    public float CurrentHealth => _currentHealth;
    public float CurrentStamina => _currentStamina;

    public float AttackPower => _attackPower;
    public float CriticalRate => _criticalRate;
    public float DefensePower => _defensePower;

    public event Action OnDead;
    public event Action<float, float, float> OnHealthChanged;
    public event Action<float, float, float> OnStaminaChanged;
    public event Action OnStatsChanged;

    public PlayerStats(PlayerData data)
    {
        // HP / スタミナ
        _maxHealth = data.Stats.MaxHealth;
        _maxStamina = data.Stats.MaxStamina;

        InitialMaxHealth = _maxHealth;
        InitialMaxStamina = _maxStamina;

        _currentHealth = _maxHealth;
        _currentStamina = _maxStamina;

        // 戦闘ステータス
        _attackPower = data.AttackPower;
        _criticalRate = data.CriticalRate;

        _defensePower = data.DefensePower;
    }

    public void TakeDamage(float damage)
    {
        _currentHealth = Mathf.Max(0, _currentHealth - damage);

        OnHealthChanged?.Invoke(_currentHealth, _maxHealth, InitialMaxHealth);

        if (_currentHealth <= 0)
        {
            OnDead?.Invoke();
        }
    }

    public void Heal(float amount)
    {
        _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);

        OnHealthChanged?.Invoke(_currentHealth, _maxHealth, InitialMaxHealth);
    }

    public bool UseStamina(float amount)
    {
        if (_currentStamina < amount)
        {
            return false;
        }

        _currentStamina = Mathf.Max(0, _currentStamina - amount);

        OnStaminaChanged?.Invoke(_currentStamina, _maxStamina, InitialMaxStamina);
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
            OnStaminaChanged?.Invoke(_currentStamina, _maxStamina, InitialMaxStamina);
        }

    }

    public void AddAttackPower(float value)
    {
        _attackPower = Mathf.Max(0f, _attackPower + value);
        OnStatsChanged?.Invoke();
    }

    public void AddCriticalRate(float value)
    {
        _criticalRate = Mathf.Max(0f, _criticalRate + value);
        OnStatsChanged?.Invoke();
    }


    public void AddDefensePower(float defensePowerBonus)
    {
        _defensePower = Mathf.Max(0f, _defensePower + defensePowerBonus);

        OnStatsChanged?.Invoke();
    }

    public void AddMaxHealth(float value)
    {
        if (value <= 0f) { return; }

        _maxHealth += value;

        _currentHealth = Mathf.Min(_currentHealth, _maxHealth);

        OnHealthChanged?.Invoke(InitialMaxHealth, _maxHealth, InitialMaxHealth);
        OnStatsChanged?.Invoke();
    }

    public void AddMaxStamina(float value)
    {
        if (value <= 0f) { return; }

        _maxStamina += value;
        _currentStamina += value;

        _currentStamina = Mathf.Min(_currentStamina, _maxStamina);

        OnStaminaChanged?.Invoke(_currentStamina, _maxStamina, InitialMaxStamina);
        OnStatsChanged?.Invoke();
    }


    private float _maxHealth;
    private float _currentHealth;

    private float _maxStamina;
    private float _currentStamina;

    private float _attackPower;
    private float _criticalRate;

    private float _defensePower;
}

