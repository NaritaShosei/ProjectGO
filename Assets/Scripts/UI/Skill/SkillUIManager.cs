using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class SkillUIManager : MonoBehaviour, ISkillSelectUIManager
{
    public event Action OnSkillSelected;
    public void Show()
    {
        _gameObject.SetActive(true);
    }

    [SerializeField] private GameObject _gameObject;

    private void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            OnSkillSelected?.Invoke();
            _gameObject.SetActive(false);
        }
    }
}
