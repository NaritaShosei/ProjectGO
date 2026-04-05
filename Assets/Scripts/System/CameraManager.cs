using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using System;

public class CameraManager : MonoBehaviour
{
    public Camera MainCamera => _mainCamera;
    public ILockOnTarget CurrentLockOnTarget { get; private set; }
    public bool IsLockedOn => CurrentLockOnTarget != null;


    public event Action<ILockOnTarget> OnLockOnTargetChanged; // ロックオン対象が変更されたときに呼び出されるイベント。

    public void Init(Player player)
    {
        if (player == null)
        {
            Debug.LogError("プレイヤーが見つかりません。CameraManagerの初期化に失敗しました。");
            return;
        }

        _playerTransform = player.transform;
        _cinemachineCamera.Follow = _playerTransform;
    }

    public void LockOn(ILockOnTarget target)
    {
        if (target == null || !target.IsLockable)
        {
            Debug.LogWarning("ロックオン対象がnullまたはロック可能ではありません。");
            return;
        }

        if (_currentTarget == target) return;

        _currentTarget = target;
        _cinemachineCamera.LookAt = _currentTarget.LockOnPoint;

        OnLockOnTargetChanged?.Invoke(_currentTarget);
    }

    public void Unlock()
    {
        if (_currentTarget == null) return;

        _currentTarget = null;
        _cinemachineCamera.LookAt = null;
        _cinemachineCamera.Follow = _playerTransform;

        OnLockOnTargetChanged?.Invoke(null);
    }

    [SerializeField] private CinemachineCamera _cinemachineCamera;
    private Camera _mainCamera;
    private Transform _playerTransform;
    private ILockOnTarget _currentTarget;

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
