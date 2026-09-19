using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// `CameraMotionController` が依存するカメラ・コンポーネント参照の組。
/// </summary>
public readonly struct CameraReferences
{
    public readonly CinemachineCamera FollowCamera;
    public readonly Transform PlayerTransform;

    public CameraReferences(CinemachineCamera followCamera, Transform playerTransform)
    {
        FollowCamera = followCamera;
        PlayerTransform = playerTransform;
    }
}

/// <summary>ロックオン対象がいない間の、スティック/マウス操作によるフリールックに関する設定値。</summary>
public readonly struct FreeLookSettings
{
    public readonly float PositionSmoothTime;
    public readonly Vector2 InputDirection;
    public readonly Vector2 RotationSpeed;

    public FreeLookSettings(
        float positionSmoothTime,
        Vector2 inputDirection,
        Vector2 rotationSpeed)
    {
        PositionSmoothTime = positionSmoothTime;
        InputDirection = inputDirection;
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
/// 常時アクティブな1台のカメラの位置・回転更新を担当します。
/// ロックオン対象がいる間はその対象を追従し、いない間はスティック/マウス操作によるフリールックに戻ります。
/// カメラの状態やロックオン対象の選定は保持しません。
/// </summary>
public sealed class CameraMotionController
{
    // ブレンド完了とみなす残り誤差
    private const float BlendCompleteAngle = 1f;      // 度
    private const float BlendCompleteDistance = 0.05f; // m

    /// <summary>
    /// カメラの動作を初期化します。
    /// </summary>
    public CameraMotionController(
        CameraReferences references,
        FreeLookSettings freeLookSettings,
        LockOnSettings lockOnSettings,
        LockOnBlendSettings blendSettings)
    {
        _followCamera = references.FollowCamera;
        _playerTransform = references.PlayerTransform;
        _orbitalFollow = _followCamera.GetComponent<CinemachineOrbitalFollow>();
        _rotationComposer = _followCamera.GetComponent<CinemachineRotationComposer>();
        _decollider = _followCamera.GetComponent<CinemachineDecollider>();

        _positionSmoothTime = freeLookSettings.PositionSmoothTime;
        _freeLookInputDirection = freeLookSettings.InputDirection;
        _freeLookRotationSpeed = freeLookSettings.RotationSpeed;

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

        SceneManager.MoveGameObjectToScene(
            _cameraFollowTarget.gameObject,
            _followCamera.gameObject.scene);

        _followCamera.Follow = _cameraFollowTarget;
        SetFreeLookComponentsEnabled(true);
    }

    /// <summary>カメラがFollowするプレイヤー追従アンカー。ボス用カメラのFollowにも流用する。</summary>
    public Transform FollowAnchor => _cameraFollowTarget;

    /// <summary>
    /// ロックオン対象がいない間の更新。追従アンカーの遅延追従と、
    /// スティック/マウス入力によるフリールック（`CinemachineOrbitalFollow`のAxis値駆動）を行う。
    /// 位置・回転自体はCinemachine（OrbitalFollow/RotationComposer/Decollider）に委ねる。
    /// </summary>
    public void UpdateFreeLook(float timeScale, Vector2 input)
    {
        _cameraFollowTarget.position = Vector3.SmoothDamp(
            _cameraFollowTarget.position,
            _playerTransform.position,
            ref _followVelocity,
            _positionSmoothTime);

        if (_orbitalFollow == null) return;
        if (input.sqrMagnitude <= 0.0001f) return;

        float deltaTime = Time.fixedDeltaTime * timeScale;
        _orbitalFollow.HorizontalAxis.Value += input.x * _freeLookInputDirection.x * _freeLookRotationSpeed.x * deltaTime;
        _orbitalFollow.VerticalAxis.Value = Mathf.Clamp(
            _orbitalFollow.VerticalAxis.Value + input.y * _freeLookInputDirection.y * _freeLookRotationSpeed.y * deltaTime,
            _orbitalFollow.VerticalAxis.Range.x,
            _orbitalFollow.VerticalAxis.Range.y);
    }

    /// <summary>ロックオン対象を設定した際に呼ぶ。フリールック用コンポーネントを無効化し、直接Transform操作へ切り替える。</summary>
    public void EnterLockOn()
    {
        SetFreeLookComponentsEnabled(false);
    }

    /// <summary>
    /// ロックオン解除時に呼ぶ。`CinemachineOrbitalFollow`のAxis値を現在のカメラ姿勢へ同期してから
    /// フリールック用コンポーネントを再有効化する。解除直後に古い向きへ飛ばないようにするため。
    /// </summary>
    public void ExitLockOn()
    {
        if (_orbitalFollow != null)
        {
            Vector3 euler = _followCamera.transform.eulerAngles;
            _orbitalFollow.HorizontalAxis.Value = euler.y;
            _orbitalFollow.VerticalAxis.Value = Mathf.Clamp(
                GetSignedPitch(euler.x),
                _orbitalFollow.VerticalAxis.Range.x,
                _orbitalFollow.VerticalAxis.Range.y);
        }

        SetFreeLookComponentsEnabled(true);
    }

    /// <summary>
    /// フリールックのカメラ姿勢をプレイヤーの現在位置・向き基準へ強制的に合わせ直す。
    /// ロックオン一時停止（ムービー再生中など）の間はカメラが非表示のまま更新が保証されないため、
    /// 再開直後に古い姿勢のまま急に映って見えるのを防ぐために呼ぶ。
    /// </summary>
    public void ResetFreeLookBehindPlayer()
    {
        _cameraFollowTarget.position = _playerTransform.position;
        _followVelocity = Vector3.zero;

        if (_orbitalFollow == null) return;

        _orbitalFollow.HorizontalAxis.Value = _playerTransform.eulerAngles.y;
        _orbitalFollow.VerticalAxis.Value = Mathf.Clamp(
            0f,
            _orbitalFollow.VerticalAxis.Range.x,
            _orbitalFollow.VerticalAxis.Range.y);
    }

    /// <summary>ロックオンカメラの位置と対象追従回転を更新します。</summary>
    public void UpdateLockOn(Camera mainCamera, Transform targetCenter)
    {
        _cameraFollowTarget.position = _playerTransform.position;

        if (_isBlending)
        {
            UpdateBlend(targetCenter);
            return;
        }

        UpdateCameraPosition();
        UpdateCameraRotation(mainCamera, targetCenter);
    }

    /// <summary>ロックオン開始・対象切り替え時のブレンドを開始する。</summary>
    /// <param name="snapFromCurrentCamera">初回ロックオンは true（現在表示中のカメラ姿勢から）、対象切り替えは false（現在のカメラ姿勢から）。</param>
    /// <param name="currentMainCamera">
    /// スナップ元にする実際の表示カメラ（Cinemachine Brainの出力Camera）。
    /// ボス戦中はこのカメラのTickが止まり `_followCamera` の姿勢が古いまま固定されるため、
    /// 未指定時のフォールバックとしてのみ `_followCamera` 自身を使う（実質スナップなし）。
    /// </param>
    public void BeginLockOnBlend(bool snapFromCurrentCamera, Camera currentMainCamera = null)
    {
        // 初回ロックオンのみ、現在実際に表示されているカメラの姿勢へスナップ（古い姿勢から飛ぶのを防ぐ）
        if (snapFromCurrentCamera)
        {
            Transform snapFrom = currentMainCamera != null
                ? currentMainCamera.transform
                : _followCamera.transform;

            _followCamera.transform.SetPositionAndRotation(
                snapFrom.position,
                snapFrom.rotation);
        }

        // 現在のカメラ姿勢をブレンド起点として記録
        _blendStartPosition = _followCamera.transform.position;
        _blendStartRotation = _followCamera.transform.rotation;
        _blendT = 0f;
        _blendElapsed = 0f;
        _isBlending = true;
    }

    /// <summary>実行中のブレンドを中止します。</summary>
    public void CancelLockOnBlend() => _isBlending = false;

    /// <summary>フリールックの位置追従の遅延・回転速度を更新します。</summary>
    public void SetFreeLookSettings(float positionSmoothTime, Vector2 rotationSpeed)
    {
        _positionSmoothTime = positionSmoothTime;
        _freeLookRotationSpeed = rotationSpeed;
    }

    /// <summary>生成したカメラ追従アンカーを破棄します。</summary>
    public void Dispose()
    {
        if (_cameraFollowTarget != null)
        {
            Object.Destroy(_cameraFollowTarget.gameObject);
        }
    }

    private readonly CinemachineCamera _followCamera;
    private readonly CinemachineOrbitalFollow _orbitalFollow;
    private readonly CinemachineRotationComposer _rotationComposer;
    private readonly CinemachineDecollider _decollider;
    private readonly Transform _playerTransform;
    private readonly Transform _cameraFollowTarget;
    private readonly Vector2 _freeLookInputDirection;
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

    private float _positionSmoothTime;
    private Vector2 _freeLookRotationSpeed;
    private Vector3 _followVelocity;

    // ロックオン開始ブレンドの実行時状態
    private bool _isBlending;
    private float _blendT;         // 0→1 の進行度（時間駆動）
    private float _blendElapsed;   // 開始からの経過秒（タイムアウト判定用）
    private Vector3 _blendStartPosition;
    private Quaternion _blendStartRotation;

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
        _followCamera.transform.position = Vector3.MoveTowards(
            _followCamera.transform.position, easedPosition, maxStepDistance);

        // 回転：イージング目標へ、最大角速度でクランプしながら回す
        Quaternion easedRotation = Quaternion.Slerp(_blendStartRotation, desiredRotation, eased);
        float maxStepDegrees = _lockOnBlendMaxAngularSpeed * Time.fixedDeltaTime;
        _followCamera.transform.rotation = Quaternion.RotateTowards(
            _followCamera.transform.rotation, easedRotation, maxStepDegrees);

        // 終了：基準時間経過＋位置・回転が収束、または追加許容時間を超過で強制終了
        bool durationElapsed = _blendT >= 1f;
        bool positionSettled =
            Vector3.Distance(_followCamera.transform.position, desiredPosition) <= BlendCompleteDistance;
        bool rotationSettled =
            Quaternion.Angle(_followCamera.transform.rotation, desiredRotation) <= BlendCompleteAngle;
        bool timedOut = _blendElapsed >= _lockOnBlendDuration + _lockOnBlendMaxExtraTime;

        if ((durationElapsed && positionSettled && rotationSettled) || timedOut)
        {
            _isBlending = false;
        }
    }

    private void UpdateCameraPosition()
    {
        _followCamera.transform.position = Vector3.MoveTowards(
            _followCamera.transform.position,
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
            _followCamera.transform.position);
        _followCamera.transform.rotation = Quaternion.Slerp(
            _followCamera.transform.rotation,
            targetRotation,
            Time.fixedDeltaTime * speed);
    }

    private Vector3 CalculateDesiredPosition()
    {
        Vector3 back = -_followCamera.transform.forward;
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
            return _followCamera.transform.rotation;
        }

        return Quaternion.LookRotation(direction);
    }

    /// <summary>Euler角のX成分（0..360）を、符号付きのピッチ角（-180..180）へ変換する。</summary>
    private static float GetSignedPitch(float rawXEuler)
    {
        return rawXEuler > 180f ? rawXEuler - 360f : rawXEuler;
    }

    /// <summary>フリールック用3コンポーネント（OrbitalFollow/RotationComposer/Decollider）の有効/無効を切り替える。</summary>
    private void SetFreeLookComponentsEnabled(bool isEnabled)
    {
        if (_orbitalFollow != null) _orbitalFollow.enabled = isEnabled;
        if (_rotationComposer != null) _rotationComposer.enabled = isEnabled;
        if (_decollider != null) _decollider.enabled = isEnabled;
    }
}
