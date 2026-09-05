using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// `CameraMotionController` が依存するカメラ・コンポーネント参照の組。
/// </summary>
public readonly struct CameraReferences
{
    public readonly CinemachineCamera NormalCamera;
    public readonly CinemachineCamera LockOnCamera;
    public readonly CinemachineOrbitalFollow NormalOrbitalFollow;
    public readonly CinemachineInputAxisController NormalInputAxisController;
    public readonly Transform PlayerTransform;

    public CameraReferences(
        CinemachineCamera normalCamera,
        CinemachineCamera lockOnCamera,
        CinemachineOrbitalFollow normalOrbitalFollow,
        CinemachineInputAxisController normalInputAxisController,
        Transform playerTransform)
    {
        NormalCamera = normalCamera;
        LockOnCamera = lockOnCamera;
        NormalOrbitalFollow = normalOrbitalFollow;
        NormalInputAxisController = normalInputAxisController;
        PlayerTransform = playerTransform;
    }
}

/// <summary>通常カメラの入力回転・追従に関する設定値。</summary>
public readonly struct NormalCameraSettings
{
    public readonly Vector2 InputDirection;
    public readonly float PositionSmoothTime;
    public readonly Vector2 RotationSpeed;

    public NormalCameraSettings(Vector2 inputDirection, float positionSmoothTime, Vector2 rotationSpeed)
    {
        InputDirection = inputDirection;
        PositionSmoothTime = positionSmoothTime;
        RotationSpeed = rotationSpeed;
    }
}

/// <summary>ロックオンカメラの位置追従・回転追従に関する設定値。</summary>
public readonly struct LockOnSettings
{
    public readonly float CameraDistance;
    public readonly float CameraHeight;
    public readonly float AreaRadius;
    public readonly float PositionSpeed;
    public readonly float FollowSpeedMin;
    public readonly float FollowSpeedMax;
    public readonly float Deadzone;

    public LockOnSettings(
        float cameraDistance,
        float cameraHeight,
        float areaRadius,
        float positionSpeed,
        float followSpeedMin,
        float followSpeedMax,
        float deadzone)
    {
        CameraDistance = cameraDistance;
        CameraHeight = cameraHeight;
        AreaRadius = areaRadius;
        PositionSpeed = positionSpeed;
        FollowSpeedMin = followSpeedMin;
        FollowSpeedMax = followSpeedMax;
        Deadzone = deadzone;
    }
}

/// <summary>ロックオン開始時のブレンドに関する設定値。</summary>
public readonly struct LockOnBlendSettings
{
    /// <summary>ブレンドの基準時間（秒）。</summary>
    public readonly float Duration;

    /// <summary>イージング（EaseOut）の指数。大きいほど序盤が速い。</summary>
    public readonly float Exponent;

    /// <summary>ブレンド中のカメラ回転の最大角速度（度/秒）。大きくズレたときだけ効く。</summary>
    public readonly float MaxAngularSpeed;

    /// <summary>ブレンド中のカメラ位置の最大移動速度（m/秒）。大きくズレたときだけ効く。</summary>
    public readonly float MaxLinearSpeed;

    /// <summary>速度上限で基準時間内に追いつかない場合の追加許容時間（秒）。超えたら強制終了。</summary>
    public readonly float MaxExtraTime;

    public LockOnBlendSettings(
        float duration,
        float exponent,
        float maxAngularSpeed,
        float maxLinearSpeed,
        float maxExtraTime)
    {
        Duration = duration;
        Exponent = exponent;
        MaxAngularSpeed = maxAngularSpeed;
        MaxLinearSpeed = maxLinearSpeed;
        MaxExtraTime = maxExtraTime;
    }
}

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
        CameraReferences references,
        NormalCameraSettings normalSettings,
        LockOnSettings lockOnSettings,
        LockOnBlendSettings blendSettings)
    {
        _normalCamera = references.NormalCamera;
        _lockOnCamera = references.LockOnCamera;
        _normalOrbitalFollow = references.NormalOrbitalFollow;
        _normalInputAxisController = references.NormalInputAxisController;
        _playerTransform = references.PlayerTransform;

        _cameraInputDirection = normalSettings.InputDirection;
        _positionSmoothTime = normalSettings.PositionSmoothTime;
        _rotationSpeed = normalSettings.RotationSpeed;

        _cameraDistance = lockOnSettings.CameraDistance;
        _cameraHeight = lockOnSettings.CameraHeight;
        _lockOnAreaRadius = lockOnSettings.AreaRadius;
        _lockOnPositionSpeed = lockOnSettings.PositionSpeed;
        _lockOnFollowSpeedMin = lockOnSettings.FollowSpeedMin;
        _lockOnFollowSpeedMax = lockOnSettings.FollowSpeedMax;
        _lockOnDeadzone = lockOnSettings.Deadzone;

        _lockOnBlendDuration = blendSettings.Duration;
        _lockOnBlendExponent = blendSettings.Exponent;
        _lockOnBlendMaxAngularSpeed = blendSettings.MaxAngularSpeed;
        _lockOnBlendMaxLinearSpeed = blendSettings.MaxLinearSpeed;
        _lockOnBlendMaxExtraTime = blendSettings.MaxExtraTime;

        _cameraFollowTarget = new GameObject("CameraFollowTarget").transform;
        _cameraFollowTarget.position = _playerTransform.position;
        _normalCamera.Follow = _cameraFollowTarget;

        SceneManager.MoveGameObjectToScene(
            _cameraFollowTarget.gameObject,
            _normalCamera.gameObject.scene);

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

    /// <summary>ロックオン開始・対象切り替え時のブレンドを開始する。</summary>
    /// <param name="snapToNormalCamera">初回ロックオンは true（通常カメラ姿勢から）、対象切り替えは false（現在のロックオン姿勢から）。</param>
    public void BeginLockOnBlend(bool snapToNormalCamera)
    {
        // 初回ロックオンのみ、通常カメラの現在姿勢へスナップ（古い姿勢から飛ぶのを防ぐ）
        if (snapToNormalCamera)
        {
            _lockOnCamera.transform.SetPositionAndRotation(
                _normalCamera.transform.position,
                _normalCamera.transform.rotation);
        }

        // 現在のロックオンカメラ姿勢をブレンド起点として記録
        _blendStartPosition = _lockOnCamera.transform.position;
        _blendStartRotation = _lockOnCamera.transform.rotation;
        _blendT = 0f;
        _blendElapsed = 0f;
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
    private readonly float _lockOnBlendMaxAngularSpeed;
    private readonly float _lockOnBlendMaxLinearSpeed;
    private readonly float _lockOnBlendMaxExtraTime;

    // ブレンド完了とみなす残り誤差
    private const float BlendCompleteAngle = 1f;      // 度
    private const float BlendCompleteDistance = 0.05f; // m

    private float _positionSmoothTime;
    private Vector2 _rotationSpeed;
    private Vector3 _normalFollowVelocity;

    // ロックオン開始ブレンドの実行時状態
    private bool _isBlending;
    private float _blendT;         // 0→1 の進行度（時間駆動）
    private float _blendElapsed;   // 開始からの経過秒（タイムアウト判定用）
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

    /// <summary>ロックオン開始ブレンドの1フレーム分の更新。イージング目標へ寄せつつ移動・回転速度を上限でクランプする。</summary>
    private void UpdateBlend(Transform targetCenter)
    {
        // 進行度と経過時間を進める
        _blendElapsed += Time.fixedDeltaTime;
        _blendT += Time.fixedDeltaTime / _lockOnBlendDuration;

        // EaseOut カーブ（序盤速く終盤ゆるやか）
        float eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(_blendT), _lockOnBlendExponent);

        // 最新の理想位置・理想回転（対象が動いても追従できるよう毎フレーム再計算）
        Vector3 desiredPosition = CalculateDesiredPosition();
        Quaternion desiredRotation = CalculateDesiredRotation(targetCenter, desiredPosition);

        // 位置：イージング目標へ、最大移動速度でクランプしながら寄せる
        Vector3 easedPosition = Vector3.Lerp(_blendStartPosition, desiredPosition, eased);
        float maxStepDistance = _lockOnBlendMaxLinearSpeed * Time.fixedDeltaTime;
        _lockOnCamera.transform.position = Vector3.MoveTowards(
            _lockOnCamera.transform.position, easedPosition, maxStepDistance);

        // 回転：イージング目標へ、最大角速度でクランプしながら回す
        Quaternion easedRotation = Quaternion.Slerp(_blendStartRotation, desiredRotation, eased);
        float maxStepDegrees = _lockOnBlendMaxAngularSpeed * Time.fixedDeltaTime;
        _lockOnCamera.transform.rotation = Quaternion.RotateTowards(
            _lockOnCamera.transform.rotation, easedRotation, maxStepDegrees);

        // 終了：基準時間経過＋位置・回転が収束、または追加許容時間を超過で強制終了
        bool durationElapsed = _blendT >= 1f;
        bool positionSettled =
            Vector3.Distance(_lockOnCamera.transform.position, desiredPosition) <= BlendCompleteDistance;
        bool rotationSettled =
            Quaternion.Angle(_lockOnCamera.transform.rotation, desiredRotation) <= BlendCompleteAngle;
        bool timedOut = _blendElapsed >= _lockOnBlendDuration + _lockOnBlendMaxExtraTime;

        if ((durationElapsed && positionSettled && rotationSettled) || timedOut)
        {
            _isBlending = false;
        }
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
