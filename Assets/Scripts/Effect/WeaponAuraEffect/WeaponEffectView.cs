using System;
using UnityEngine;

public class WeaponEffectView : MonoBehaviour
{
    public void Change(PlayerMode mode)
    {
        var next = mode switch
        {
            PlayerMode.Thunder => _thunderEffect as IWeaponEffect,
            PlayerMode.Warrior => _warriorEffect as IWeaponEffect,
            _ => throw new ArgumentOutOfRangeException()
        };

        if (next == _currentEffect)
            return;
        if (next == null)
            Debug.LogError("IWeaponEffectが設定されてない");

        _currentEffect?.Stop();
        _currentEffect = next;
        _currentEffect?.Play();
    }

    [SerializeField] private MonoBehaviour _thunderEffect;
    [SerializeField] private MonoBehaviour _warriorEffect;

    private IWeaponEffect _currentEffect;
    private void Awake()
    {
        (_thunderEffect as IWeaponEffect)?.Stop();
        (_warriorEffect as IWeaponEffect)?.Stop();
    }
}
