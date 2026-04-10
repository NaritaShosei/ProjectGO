using System;
using Unity.Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public Camera MainCamera => _mainCamera;
    public ILockOnTarget CurrentTarget => _currentTarget;
    public bool IsLockedOn => _currentTargetComponent != null;

    public event Action<ILockOnTarget> OnLockOnTargetChanged;

    public void Init(Player player)
    {
        if (player == null)
        {
            Debug.LogError("Playerの参照がnullです。");
            return;
        }

        _playerTransform = player.transform;
        _normalCamera.Follow = _playerTransform;
    }

    public void LockOn(ILockOnTarget target)
    {
        if (target is not Component targetComponent || !targetComponent || !target.IsLockable || target.LockOnPoint == null)
        {
            Debug.LogWarning("ロックオン対象がnullまたはロック不可です。");
            return;
        }

        if (_currentTarget == target) return;

        _currentTarget = target;
        _currentTargetComponent = targetComponent;
        _lockOnCamera.Priority = _lockOnPriority;

        OnLockOnTargetChanged?.Invoke(_currentTarget);
    }

    public void Unlock()
    {
        if (_currentTarget == null && _currentTargetComponent == null) return;

        _currentTarget = null;
        _currentTargetComponent = null;

        // ロックオンカメラの現在の角度を通常カメラに引き継ぐ
        Vector3 currentEuler = _lockOnCamera.transform.rotation.eulerAngles;
        _normalCamera.GetComponent<CinemachineOrbitalFollow>().HorizontalAxis.Value = currentEuler.y;
        _normalCamera.GetComponent<CinemachineOrbitalFollow>().VerticalAxis.Value = currentEuler.x;

        _lockOnCamera.Priority = _normalPriority - 1;

        OnLockOnTargetChanged?.Invoke(null);
    }

    [Header("カメラ")]
    [SerializeField] private CinemachineCamera _normalCamera;
    [SerializeField] private CinemachineCamera _lockOnCamera;

    [Header("Priority設定")]
    [SerializeField] private int _normalPriority = 10;
    [SerializeField] private int _lockOnPriority = 20;

    [Header("カメラオフセット設定")]
    [SerializeField] private float _cameraDistance = 5f;
    [SerializeField] private float _cameraHeight = 2f;
    [SerializeField] private float _followSpeed = 10f;

    private Camera _mainCamera;
    private Transform _playerTransform;
    private ILockOnTarget _currentTarget;
    private Component _currentTargetComponent;

    private void Awake()
    {
        _mainCamera = Camera.main;
        ServiceLocator.Register(this);

        _normalCamera.Priority = _normalPriority;
        _lockOnCamera.Priority = _normalPriority - 1;
    }

    private void FixedUpdate()
    {
        if (_currentTargetComponent == null || !_currentTarget.IsLockable || _currentTarget.LockOnPoint == null)
        {
            Unlock();
            return;
        }

        UpdateLockOnCamera();
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<CameraManager>();
    }

    private void UpdateLockOnCamera()
    {
        // 敵→プレイヤーの方向を計算（Y軸無視）
        Vector3 dirToPlayer = _playerTransform.position - _currentTarget.LockOnPoint.position;
        dirToPlayer.y = 0f;

        if (dirToPlayer.sqrMagnitude < 0.001f) return;

        dirToPlayer.Normalize();

        // プレイヤーの背後の位置を計算
        Vector3 targetPosition = _playerTransform.position
            + dirToPlayer * _cameraDistance
            + Vector3.up * _cameraHeight;

        // カメラ位置を滑らかに移動
        _lockOnCamera.transform.position = Vector3.Lerp(
            _lockOnCamera.transform.position,
            targetPosition,
            Time.deltaTime * _followSpeed
        );

        // プレイヤーと敵の中間点を向く
        Vector3 lookAtPoint = Vector3.Lerp(
            _playerTransform.position,
            _currentTarget.LockOnPoint.position,
            0.5f
        );

        _lockOnCamera.transform.rotation = Quaternion.Lerp(
            _lockOnCamera.transform.rotation,
            Quaternion.LookRotation(lookAtPoint - _lockOnCamera.transform.position),
            Time.deltaTime * _followSpeed
        );
    }
}
