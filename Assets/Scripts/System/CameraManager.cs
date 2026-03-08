using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public Camera MainCamera => _mainCamera;

    public void Init(Player player)
    {
        if (player == null)
        {
            Debug.LogError("Player reference is null in CameraManager.Init.");
            return;
        }

        _playerTransform = player.transform;
        _cinemachineCamera.Follow = _playerTransform;
    }

    public void LockOn(Transform target)
    {
        if (target == null)
        {
            Debug.LogWarning("LockOn target is null.");
            return;
        }
    }

    public void Unlock()
    {
        _cinemachineCamera.Follow = _playerTransform;
    }

    [SerializeField] private CinemachineCamera _cinemachineCamera;
    private Camera _mainCamera;
    private Transform _playerTransform;

    private void Awake()
    {
        _mainCamera = Camera.main;

        ServiceLocator.Register(this);
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<CameraManager>();
    }
}
