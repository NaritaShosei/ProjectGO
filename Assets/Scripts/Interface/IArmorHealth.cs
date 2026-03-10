using System;
using UnityEngine;

public interface IArmorHealth
{
    event Action<float, float> OnHealthChanged; // (current, max)
    event Action OnBroken;
    Transform GetTargetCenter();
}
