using System;
using UnityEngine;

public class WeaponEffectView : MonoBehaviour
{
    public void Change(PlayerMode mode)
    {
        _thunderEffect.SetActive(false);
        _warriorEffect.SetActive(false);
        switch (mode)
        {
            case PlayerMode.Thunder:
                _thunderEffect.SetActive(true);
                break;
            case PlayerMode.Warrior:
                _warriorEffect.SetActive(true);
                break;
            default: throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
        }
    }

    [SerializeField] private GameObject _thunderEffect;
    [SerializeField] private GameObject _warriorEffect;

    private void Awake()
    {
        _thunderEffect.SetActive(false);
        _warriorEffect.SetActive(false);
    }
}
