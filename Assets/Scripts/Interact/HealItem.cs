using System;
using UnityEngine;

public class HealItem : MonoBehaviour, IInteractable
{
    public event Action<HealItem> OnInteracted;

    public void Interact(GameObject interactor)
    {
        if (interactor.TryGetComponent(out IHealth component))
        {
            component.Healing(_healValue);
            OnInteracted?.Invoke(this);
        }
    }

    public void ResetItem()
    {
        gameObject.SetActive(false);
    }

    [Header("回復量")]
    [SerializeField] private float _healValue = 50;
}
