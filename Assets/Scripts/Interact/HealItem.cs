using System;
using UnityEngine;

public class HealItem : MonoBehaviour, IInteractable, IPoolable
{
    public event Action<HealItem> OnInteracted;

    // ── IPoolable ────────────────────────────────────────────

    /// <summary>プールから取り出された直後。イベント購読をクリアして再利用に備える。</summary>
    public void OnGet()
    {
        OnInteracted = null;
    }

    /// <summary>プールへ返却される直前。GameObject を非表示にする。</summary>
    public void OnRelease()
    {
        gameObject.SetActive(false);
    }

    // ── IInteractable ────────────────────────────────────────

    public void Interact(GameObject interactor)
    {
        if (interactor.TryGetComponent(out IHealth component))
        {
            component.Healing(_healValue);
            OnInteracted?.Invoke(this);
        }
    }

    // ── Inspector ────────────────────────────────────────────

    [Header("回復量")]
    [SerializeField] private float _healValue = 50;
}
