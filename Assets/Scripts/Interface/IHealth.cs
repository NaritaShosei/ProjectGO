using UnityEngine;

public interface IHealth
{
    public void Healing(float amount);
    public void TakeDamage(float damage);
}