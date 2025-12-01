using UnityEngine;

public class HealItem : MonoBehaviour, IInteractable
{
    [Header("UI表示する説明")]
    [SerializeField] private string _exception;
    [Header("回復量")]
    [SerializeField] private float _healValue = 50;

    public string GetInteractText()
    {
        return _exception;
    }

    public void Interact(GameObject interactor)
    {
        if (interactor.TryGetComponent(out IHealth component))
        {
            component.Healing(_healValue);
            OnInteracted();
        }
    }

    private void OnInteracted()
    {
        // とりあえず破壊
        Destroy(gameObject);
    }
}
