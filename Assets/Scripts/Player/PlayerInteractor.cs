using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    public void Init(PlayerStateManager playerStateManager, InputHandler input)
    {
        _playerStateManager = playerStateManager;
        _input = input;

        _input.OnInteract += HandleInteract;

        _interactHandler = ServiceLocator.Get<ItemPickupManager>();
    }

    public async UniTaskVoid SearchLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            Search();
            await UniTask.Delay(
                System.TimeSpan.FromSeconds(_interactSearchInterval),
                cancellationToken: token
            );
        }
    }


    [Header("インタラクト")]
    [SerializeField] private float _interactSearchInterval = 0.1f;
    [SerializeField] private float _interactSearchRadius = 5f;
    [SerializeField] private float _interactSearchRange = 1f;
    [SerializeField] private LayerMask _interactableLayer;

    private IItemInteractHandler _interactHandler;
    private IInteractable _nearestInteractable;
    private PlayerStateManager _playerStateManager;
    private InputHandler _input;

    private void OnDestroy()
    {
        if (_input != null)
        {
            _input.OnInteract -= HandleInteract;
        }
    }

    private void Search()
    {
        var hits = Physics.OverlapSphere(
            transform.position + transform.forward * _interactSearchRange,
            _interactSearchRadius,
            _interactableLayer
        );

        IInteractable found = null;
        float bestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent(out IInteractable interactable)) continue;
            float dist = (hit.transform.position - transform.position).sqrMagnitude;
            if (dist < bestDist)
            {
                bestDist = dist;
                found = interactable;
            }
        }

        if (found == _nearestInteractable) return;

        if (_nearestInteractable != null)
            _interactHandler?.ClearNearTarget(_nearestInteractable);

        _nearestInteractable = found;

        if (_nearestInteractable != null)
            _interactHandler?.SetNearTarget(_nearestInteractable);
    }

    private void HandleInteract()
    {
        //インタラクトの状況をログに出す（デバッグ用）
        Debug.Log($"Interact pressed. CanInteract: {_playerStateManager.CanInteract()}, Nearest: {_nearestInteractable?.GetType().Name ?? "None"}");
        // 攻撃中・回避中など行動制限がある状態ではインタラクトしない
        if (!_playerStateManager.CanInteract()) return;
        if (_nearestInteractable == null) return;

        _interactHandler?.OnPlayerInteract(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        Vector3 center = transform.position + transform.forward * _interactSearchRange;

        Gizmos.DrawWireSphere(center, _interactSearchRadius);
    }
}
