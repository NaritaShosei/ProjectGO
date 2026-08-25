using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// 通常カメラとロックオンカメラの位置・回転更新を担当します。
/// カメラの状態やロックオン対象の選定は保持しません。
/// </summary>
public sealed class CameraMotionController
{
    /// <summary>
    /// 通常カメラとロックオンカメラの動作を初期化します。
    /// </summary>
    public CameraMotionController(
        CinemachineCamera normalCamera,
        CinemachineCamera lockOnCamera,
        CinemachineOrbitalFollow normalOrbitalFollow,
        CinemachineInputAxisController normalInputAxisController,
        Transform playerTransform,
        Vector2 cameraInputDirection,
        float cameraDistance,
        float cameraHeight,
        float positionSmoothTime,
        Vector2 rotationSpeed,
        float lockOnAreaRadius,
        float lockOnPositionSpeed,
        float lockOnFollowSpeedMin,
        float lockOnFollowSpeedMax,
        float lockOnDeadzone,
        float lockOnBlendDuration,
        float lockOnBlendExponent)
    {
        _normalCamera = normalCamera;
        _lockOnCamera = lockOnCamera;
        _normalOrbitalFollow = normalOrbitalFollow;
        _normalInputAxisController = normalInputAxisController;
        _playerTransform = playerTransform;
        _cameraInputDirection = cameraInputDirection;
        _cameraDistance = cameraDistance;
        _cameraHeight = cameraHeight;
        _positionSmoothTime = positionSmoothTime;
        _rotationSpeed = rotationSpeed;
        _lockOnAreaRadius = lockOnAreaRadius;
        _lockOnPositionSpeed = lockOnPositionSpeed;
        _lockOnFollowSpeedMin = lockOnFollowSpeedMin;
        _lockOnFollowSpeedMax = lockOnFollowSpeedMax;
        _lockOnDeadzone = lockOnDeadzone;
        _lockOnBlendDuration = lockOnBlendDuration;
        _lockOnBlendExponent = lockOnBlendExponent;

        _cameraFollowTarget = new GameObject("CameraFollowTarget").transform;
        _cameraFollowTarget.position = _playerTransform.position;
        _normalCamera.Follow = _cameraFollowTarget;

        if (_normalInputAxisController != null)
        {
            _normalInputAxisController.enabled = false;
        }
    }

    /// <summary>通常カメラの入力回転とプレイヤー追従を更新します。</summary>
    public void UpdateNormal(float timeScale, Vector2 input)
    {
        UpdateFreeCameraRotation(timeScale, input);
        _cameraFollowTarget.position = Vector3.SmoothDamp(
            _cameraFollowTarget.position,
            _playerTransform.position,
            ref _normalFollowVelocity,
            _positionSmoothTime);
    }

    /// <summary>ロックオンカメラの位置と対象追従回転を更新します。</summary>
    public void UpdateLockOn(Camera mainCamera, Transform targetCenter)
    {
        if (_isBlending)
        {
            UpdateBlend(targetCenter);
            return;
        }

        UpdateCameraPosition();
        UpdateCameraRotation(mainCamera, targetCenter);
        _cameraFollowTarget.position = _playerTransform.position;
    }

    /// <summary>ロックオン開始時のブレンドを開始します。</summary>
    public void BeginLockOnBlend()
    {
        _blendStartPosition = _lockOnCamera.transform.position;
        _blendStartRotation = _lockOnCamera.transform.rotation;
        _blendT = 0f;
        _isBlending = true;
    }

    /// <summary>実行中のロックオンブレンドを中止します。</summary>
    public void CancelLockOnBlend() => _isBlending = false;

    /// <summary>ロックオンカメラの角度を通常カメラへ引き継ぎます。</summary>
    public void ApplyRotationToNormalCamera()
    {
        if (_normalOrbitalFollow == null) return;

        Vector3 euler = _lockOnCamera.transform.rotation.eulerAngles;
        _normalOrbitalFollow.HorizontalAxis.Value = euler.y;
        _normalOrbitalFollow.VerticalAxis.Value = euler.x;
    }

    /// <summary>通常カメラの移動遅延と回転速度を更新します。</summary>
    public void SetNormalSettings(float positionSmoothTime, Vector2 rotationSpeed)
    {
        _positionSmoothTime = positionSmoothTime;
        _rotationSpeed = rotationSpeed;
    }

    /// <summary>生成したカメラ追従アンカーを破棄します。</summary>
    public void Dispose()
    {
        if (_cameraFollowTarget != null)
        {
            Object.Destroy(_cameraFollowTarget.gameObject);
        }
    }

    private readonly CinemachineCamera _normalCamera;
    private readonly CinemachineCamera _lockOnCamera;
    private readonly CinemachineOrbitalFollow _normalOrbitalFollow;
    private readonly CinemachineInputAxisController _normalInputAxisController;
    private readonly Transform _playerTransform;
    private readonly Transform _cameraFollowTarget;
    private readonly Vector2 _cameraInputDirection;
    private readonly float _cameraDistance;
    private readonly float _cameraHeight;
    private readonly float _lockOnAreaRadius;
    private readonly float _lockOnPositionSpeed;
    private readonly float _lockOnFollowSpeedMin;
    private readonly float _lockOnFollowSpeedMax;
    private readonly float _lockOnDeadzone;
    private readonly float _lockOnBlendDuration;
    private readonly float _lockOnBlendExponent;

    private float _positionSmoothTime;
    private Vector2 _rotationSpeed;
    private Vector3 _normalFollowVelocity;
    private bool _isBlending;
    private float _blendT;
    private Vector3 _blendStartPosition;
    private Quaternion _blendStartRotation;

    private void UpdateFreeCameraRotation(float timeScale, Vector2 input)
    {
        if (_normalOrbitalFollow == null) return;
        if (input.sqrMagnitude <= 0.0001f) return;

        Vector2 rotationDelta = new(
            input.x * _cameraInputDirection.x * _rotationSpeed.x,
            input.y * _cameraInputDirection.y * _rotationSpeed.y);
        float deltaTime = Time.fixedDeltaTime * timeScale;

        _normalOrbitalFollow.HorizontalAxis.Value += rotationDelta.x * deltaTime;
        _normalOrbitalFollow.VerticalAxis.Value = Mathf.Clamp(
            _normalOrbitalFollow.VerticalAxis.Value + rotationDelta.y * deltaTime,
            _normalOrbitalFollow.VerticalAxis.Range.x,
            _normalOrbitalFollow.VerticalAxis.Range.y);
    }

    private void UpdateBlend(Transform targetCenter)
    {
        _blendT += Time.fixedDeltaTime / _lockOnBlendDuration;
        float eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(_blendT), _lockOnBlendExponent);
        Vector3 desiredPosition = CalculateDesiredPosition();
        Quaternion desiredRotation = CalculateDesiredRotation(targetCenter, desiredPosition);

        _lockOnCamera.transform.position = Vector3.Lerp(_blendStartPosition, desiredPosition, eased);
        _lockOnCamera.transform.rotation = Quaternion.Slerp(_blendStartRotation, desiredRotation, eased);

        if (_blendT >= 1f) _isBlending = false;
    }

    private void UpdateCameraPosition()
    {
        _lockOnCamera.transform.position = Vector3.MoveTowards(
            _lockOnCamera.transform.position,
            CalculateDesiredPosition(),
            _lockOnPositionSpeed * Time.fixedDeltaTime);
    }

    private void UpdateCameraRotation(Camera mainCamera, Transform targetCenter)
    {
        Vector3 rawScreenPosition = mainCamera.WorldToScreenPoint(targetCenter.position);
        if (rawScreenPosition.z < 0)
        {
            RotateToward(targetCenter, _lockOnFollowSpeedMax);
            return;
        }

        Vector2 screenCenter = new(Screen.width / 2f, Screen.height / 2f);
        float deviation = Vector2.Distance(
            new Vector2(rawScreenPosition.x, rawScreenPosition.y), screenCenter);
        if (deviation <= _lockOnAreaRadius) return;

        float deviationFromArea = deviation - _lockOnAreaRadius;
        float speed = Mathf.Lerp(
            _lockOnFollowSpeedMin,
            _lockOnFollowSpeedMax,
            Mathf.Clamp01(deviationFromArea / _lockOnDeadzone));
        RotateToward(targetCenter, speed);
    }

    private void RotateToward(Transform targetCenter, float speed)
    {
        Quaternion targetRotation = CalculateDesiredRotation(
            targetCenter,
            _lockOnCamera.transform.position);
        _lockOnCamera.transform.rotation = Quaternion.Slerp(
            _lockOnCamera.transform.rotation,
            targetRotation,
            Time.fixedDeltaTime * speed);
    }

    private Vector3 CalculateDesiredPosition()
    {
        Vector3 back = -_lockOnCamera.transform.forward;
        back.y = 0f;
        back.Normalize();

        return _playerTransform.position
            + back * _cameraDistance
            + Vector3.up * _cameraHeight;
    }

    private Quaternion CalculateDesiredRotation(Transform targetCenter, Vector3 fromPosition)
    {
        Vector3 lookAtPoint = Vector3.Lerp(
            _playerTransform.position,
            targetCenter.position,
            0.5f);
        Vector3 direction = lookAtPoint - fromPosition;
        if (direction.sqrMagnitude < 0.001f)
        {
            return _lockOnCamera.transform.rotation;
        }

        return Quaternion.LookRotation(direction);
    }
}