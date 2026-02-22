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

        _cinemachineTargetGroup.Targets[0].Object = _playerTransform;
    }

    public void LockOn(Transform target)
    {
        if (target == null)
        {
            Debug.LogWarning("LockOn target is null.");
            return;
        }

        _cinemachineTargetGroup.Targets[1].Object = target;

        _cinemachineCamera.Follow = _cinemachineTargetGroup.transform;
    }

    public void Unlock()
    {
        _cinemachineTargetGroup.Targets[1].Object = null;
        _cinemachineCamera.Follow = _playerTransform;
    }

    [SerializeField] private CinemachineCamera _cinemachineCamera;
    [SerializeField] private CinemachineTargetGroup _cinemachineTargetGroup;
    private Camera _mainCamera;
    private Transform _playerTransform;

    private void Awake()
    {
        _mainCamera = Camera.main;
    }
}
