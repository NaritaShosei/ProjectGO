using System;
using UnityEngine;

/// <summary>
/// EnemyStatsと同じ役割として作成
/// EnemyStatsと同じでも可？
/// もしHP減少Actionを登録したいなら作り変える
/// </summary>
public class ArmorStats
{

    public event Action OnBroken;

    public float CurrentHealth => _currentHealth;

    public ArmorStats(ArmorData data)
    {
        _maxHealth = data.MaxHP;
        _currentHealth = _maxHealth;
    }

    public void TakeDamage(float damage)
    {
        _currentHealth = Mathf.Max(0, _currentHealth - damage);

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
