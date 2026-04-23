using System;
using UnityEngine;

public class WeaponEffectView : MonoBehaviour
{
    public void Change(PlayerMode mode)
    {
        Debug.Log("View起動");
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

    [SerializeField] private Transform _vfxSocket;
    [SerializeField] private GameObject _thunderPrefab;
    [SerializeField] private GameObject _warriorPrefab;

    private GameObject _thunderEffect;
    private GameObject _warriorEffect;

    private void Awake()
    {
        _thunderEffect = Instantiate(_thunderPrefab, _vfxSocket);
        _warriorEffect = Instantiate(_warriorPrefab, _vfxSocket);

        _thunderEffect.SetActive(false);
        _warriorEffect.SetActive(false);
    }
}
