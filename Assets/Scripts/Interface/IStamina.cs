using System;
using UnityEngine;

public interface IStamina
{
    event Action<float, float, float> OnStaminaChanged;
    public bool TryUseStamina(float amount);
    public float GetDodgeStaminaCost();
}
