using System;
using UnityEngine;

/// <summary>
/// EnemyのHPを管理するクラス
/// </summary>
public class EnemyStats : IHealthStats
{
    public event Action OnDead;
    public event Action OnHealthZero;
    public event Action<float, float> OnHealthChanged;

    public float MaxHealth { get => _maxHealth; }
    public float CurrentHealth { get => _currentHealth; }

    public EnemyStats(EnemyData data)
    {
        _maxHealth = data.MaxHP;
        _currentHealth = _maxHealth;
    }

    /// <summary>ダメージを与えてHPを減らす。0以下になった場合はOnHealthZeroを発火する</summary>
    public void TakeDamage(float damage)
    {
        _currentHealth = Mathf.Max(0, _currentHealth - damage);
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

        if (_currentHealth <= 0)
        {
            OnHealthZero?.Invoke();
        }
    }

    /// <summary>OnDeadを発火して死亡処理をトリガーする</summary>
    public void Kill()
    {
        OnDead?.Invoke();
    }

    /// <summary>MaxHPを更新してHPを全回復する（リスポーン等）</summary>
    public void ResetHP(float maxHP)
    {
        _maxHealth = maxHP;
        _currentHealth = _maxHealth;
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
    }

    private float _maxHealth;
    private float _currentHealth;
}
