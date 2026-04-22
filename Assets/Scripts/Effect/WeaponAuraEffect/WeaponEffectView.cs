using System;
using UnityEngine;

public class WeaponEffectView : MonoBehaviour
{
    public void Change(PlayerMode mode)
    {
        _thunderInstance.SetActive(false);
        _warriorInstance.SetActive(false);
        switch (mode)
        {
            case PlayerMode.Thunder:
                _thunderInstance.SetActive(true);
                break;
            case PlayerMode.Warrior:
                _warriorInstance.SetActive(true);
                break;
            default: throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
        }
    }

    [SerializeField] private Transform _vfxSocket;
    [SerializeField] private GameObject _thunderPrefab;
    [SerializeField] private GameObject _warriorPrefab;

    private GameObject _thunderInstance;
    private GameObject _warriorInstance;

    private void Awake()
    {
        _thunderInstance = Instantiate(_thunderPrefab, _vfxSocket);
        _warriorInstance = Instantiate(_warriorPrefab, _vfxSocket);

        _thunderInstance.SetActive(false);
        _warriorInstance.SetActive(false);
    }
}
