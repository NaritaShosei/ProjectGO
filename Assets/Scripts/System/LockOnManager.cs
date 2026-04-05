using UnityEngine;

public class LockOnManager : MonoBehaviour
{
    [SerializeField] private float _lockOnRange = 20f;
    [SerializeField] private LayerMask _lockOnLayer;
    [SerializeField] private Player _player; //　一時的に直接プレイヤー参照するS

    private CameraManager _cameraManager;
    private Transform _playerTransform;
    private readonly Collider[] _overlapBuffer = new Collider[32];

    private void Awake()
    {
        ServiceLocator.Register(this);
    }

    private void Start()
    {
        _cameraManager = ServiceLocator.Get<CameraManager>();
        _playerTransform = _player.transform;
        // player = ServiceLocator.Get<Player>();
        // _playerTransform = player.Transform;
        // if (player != null)
        // {
        //     _playerTransform = player.Transform;
        // }
        // else
        // {
        //     Debug.LogError("Playerが見つかりません。");
        // }

        InputHandler inputHandler = ServiceLocator.Get<InputHandler>();
        inputHandler.OnLockOn += ToggleLockOn;
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<LockOnManager>();

        InputHandler inputHandler = ServiceLocator.Get<InputHandler>();
        if (inputHandler != null)
        {
            inputHandler.OnLockOn -= ToggleLockOn;
        }
    }

    private void ToggleLockOn()
    {
        if (_cameraManager.IsLockedOn)
        {
            _cameraManager.Unlock();
        }
        else
        {
            TryLockOn();
        }
    }

    private void TryLockOn()
    {
        ILockOnTarget nearest = FindNearestTarget();
        if (nearest == null) return;

        _cameraManager.LockOn(nearest);
    }

    private ILockOnTarget FindNearestTarget()
    {
        int hitCount = Physics.OverlapSphereNonAlloc(
            _playerTransform.position,
            _lockOnRange,
            _overlapBuffer,
            _lockOnLayer
        );

        ILockOnTarget nearest = null;
        float nearestDist = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            if (!_overlapBuffer[i].TryGetComponent(out ILockOnTarget candidate)) continue;
            if (!candidate.IsLockable) continue;

            float dist = Vector3.Distance(
                _playerTransform.position,
                candidate.LockOnPoint.position
            );

            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = candidate;
            }
        }

        return nearest;
    }
}