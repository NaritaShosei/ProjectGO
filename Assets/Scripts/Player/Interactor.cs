using System;
using UnityEngine;

public class Interactor : MonoBehaviour
{
    [Header("判定可能な距離")]
    [SerializeField] private float _interactRange = 2f;
    [Header("判定可能な半径")]
    [SerializeField] private float _interactRadius = 2f;
    [Header("インタラクト可能なレイヤー")]
    [SerializeField] private LayerMask _interactableLayer;

    private IInteractable _currentInteractable;

    public void Init(InputHandler input)
    {
        input.OnInteract += Interact;
    }

    private void Interact()
    {
        if (_currentInteractable == null) { return; }

        // TODO:一旦雑に条件設定
        if (!FindAnyObjectByType<PlayerManager>().HasFlag(
            PlayerStateFlags.Attacking |
            PlayerStateFlags.Dodging |
            PlayerStateFlags.Dead |
            PlayerStateFlags.Charging |
            PlayerStateFlags.ModeChange
            ))

            _currentInteractable.Interact(gameObject);
    }

    private void Update()
    {
        // 毎フレームインタラクトできるオブジェクトを探索
        // TODO:効率悪くね
        DetectInteractable();
    }

    private void DetectInteractable()
    {
        var pos = transform.position + transform.forward * _interactRange;
        Collider[] colls = Physics.OverlapSphere(pos, _interactRadius, _interactableLayer);

        IInteractable selected = null;
        float selectedDistance = float.MaxValue;

        foreach (var col in colls)
        {
            if (col.TryGetComponent(out IInteractable interactable))
            {
                float distance = Vector3.Distance(transform.position, col.transform.position);
                if (distance < selectedDistance)
                {
                    selectedDistance = distance;
                    selected = interactable;
                }
            }
        }

        _currentInteractable = selected;

        UpdateInteractUI();
    }

    private void UpdateInteractUI()
    {
        if (_currentInteractable != null)
        {
            // TODO:UI表示(〇:回復)のようなテキストUI
            // _currentInteractable.GetInteractText();
        }
    }

#if  UNITY_EDITOR

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + transform.forward * _interactRange, _interactRadius);
    }

#endif
}
