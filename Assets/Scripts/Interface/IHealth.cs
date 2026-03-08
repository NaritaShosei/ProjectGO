using System;
using UnityEngine;

public interface IHealth
{
    public event Action<float, float, float> OnHealthChanged;
    public void Healing(float amount);
    public void TakeDamage(float damage);
}
