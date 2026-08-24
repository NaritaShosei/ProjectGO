using System;
using Cysharp.Threading.Tasks;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// カメラの挙動を管理するクラス。
/// 通常時の追従遅延およびロックオン時のターゲット追従を制御します。
/// ターゲット選定はLockOnControllerに委譲しています。
/// </summary>
public class CameraManager : MonoBehaviour, ISpeedChange
{
    #region パブリックプロパティ・イベント

    /// <summary>メインカメラの参照</summary>
    public Camera MainCamera
    {
        get
        {
            if (_mainCamera == null)
            {
                RefreshMainCamera(SceneManager.GetActiveScene());
            }

            return _mainCamera;
        }
    }

    /// <summary>現在ロックオンしている対象</summary>
    public ILockOnTarget CurrentTarget => _currentTarget;

    /// <summary>現在ロックオン中かどうか</summary>
    public bool IsLockedOn => _currentTarget != null;

    public float TimeScale => _timeScale;

    /// <summary>ロックオンエリアの半径（px）</summary>
    public float LockOnAreaRadius => _lockOnAreaRadius;

    /// <summary>ロックオン対象が変更された際の通知</summary>
    public event Action<ILockOnTarget> OnLockOnTargetChanged;

    #endregion

    #region パブリックメソッド

    /// <summary>
    /// カメラマネージャーの初期化。
    /// 追従対象のプレイヤーを登録し、通常カメラのFollowターゲットを設定します。
    /// </summary>
    public void Init(Player player)
    {
        if (player == null)
        {
            Debug.LogError("Playerの参照がnullです。");
            return;
        }

        if (_cameraFollowTarget == null || _normalCamera == null || _lockOnCamera == null || _lockOnController == null)
        {
            Debug.LogError("[CameraManager] Required camera references are missing.", this);
            return;
        }

        _playerTransform = player.transform;
        _cameraFollowTarget.position = _playerTransform.position;
        _normalCamera.Follow = _cameraFollowTarget;

        if (ServiceLocator.TryGet(out HitStopManager hitStopManager))
        {
            hitStopManager.Register(this, HitStopTargetGroup.Camera);
        }

        if (ServiceLocator.TryGet(out InputHandler inputHandler) && ServiceLocator.TryGet(out EnemyManager enemyManager))
        {
            _inputHandler = inputHandler;
            _lockOnController.Init(this, inputHandler, enemyManager, _playerTransform);
        }
        else
        {
            Debug.LogError("[CameraManager] InputHandler or EnemyManager is missing. LockOn is disabled.", this);
        }

        if (_occlusionManager != null)
        {
            _occlusionManager.Init(_playerTransform);
        }
    }

    /// <summary>
    /// 指定したターゲットをロックオンします。
    /// ロックオン開始時はEaseOutブレンドで滑らかにカメラを切り替えます。
    /// </summary>
    public void LockOn(ILockOnTarget target)
    {
        if (!IsValidTarget(target)) return;
        if (_currentTarget == target) return;

        _currentTarget = target;
        _lockOnCamera.Priority = _lockOnPriority;

        BeginLockOnBlend();

        Debug.Log(_currentTarget);
        OnLockOnTargetChanged?.Invoke(_currentTarget);
    }

    /// <summary>
    /// ロックオンを解除し、通常カメラに戻します。
    /// 解除時は現在のカメラ角度を通常カメラに引き継ぎます。
    /// </summary>
    public void Unlock()
    {
        if (_currentTarget == null) return;

        ApplyRotationToNormalCamera();

        _currentTarget = null;
        _lockOnCamera.Priority = _normalPriority - 1;
        _isLockOnBlending = false;

        OnLockOnTargetChanged?.Invoke(null);
    }

    /// <summary>カメラシェイクを実行します。</summary>
    public async UniTask ExecutionCameraShake(CameraShakeData data)
    {
        var camera = IsLockedOn ? _lockOnCamera : _normalCamera;

        await _cameraShake.StartCameraShake(camera, data);
    }

    /// <summary>カメラシェイクを強制停止します。</summary>
    public void ExecutionForceStopCameraShake()
    {
        _cameraShake.ForceStopCameraShake();
    }

    #endregion

    #region Inspectorフィールド

    [Header("カメラ参照")]
    [Tooltip("通常時に使用するCinemachineカメラ")]
    [SerializeField] private CinemachineCamera _normalCamera;
    [Tooltip("ロックオン時に使用するCinemachineカメラ")]
    [SerializeField] private CinemachineCamera _lockOnCamera;

    [Header("カメラの遮蔽判定")]
    [SerializeField] private OcclusionManager _occlusionManager;

    [Header("優先度設定")]
    [Tooltip("通常カメラのPriority。ロックオンカメラはこれより低い値で待機する")]
    [SerializeField] private int _normalPriority = 10;
    [Tooltip("ロックオン時に設定するPriority。通常カメラより高くする必要がある")]
    [SerializeField] private int _lockOnPriority = 20;

    [Header("通常追従設定")]
    [Tooltip("プレイヤー真後ろからのカメラ距離（m）")]
    [SerializeField] private float _cameraDistance = 5f;
    [Tooltip("プレイヤーからのカメラの高さ（m）")]
    [SerializeField] private float _cameraHeight = 2f;
    [Tooltip("通常時のカメラ位置追従の遅延時間（秒）。大きいほど追従がゆっくりになる")]
    [SerializeField] private float _posSmoothTime = 0.2f;

    [Header("フリーカメラ入力")]
    [SerializeField] private Vector2 _cameraRotationSpeed = new(120f, 80f);
    [SerializeField] private Vector2 _cameraInputDirection = new(1f, -1f);

    [Header("ロックオン設定")]
    [Tooltip("ターゲットがこの半径（px）内に収まっている間はカメラが回転しないデッドゾーン")]
    [SerializeField] private float _lockOnAreaRadius = 100f;
    [Tooltip("カメラ位置の目標追従速度（m/s）")]
    [SerializeField] private float _lockOnPositionSpeed = 10f;
    [Tooltip("ターゲットがデッドゾーンを出た直後のカメラ回転速度の最小値")]
    [SerializeField] private float _lockOnFollowSpeedMin = 2f;
    [Tooltip("ターゲットが大きく逸脱したときのカメラ回転速度の最大値")]
    [SerializeField] private float _lockOnFollowSpeedMax = 10f;
    [Tooltip("デッドゾーン境界から最大速度に達するまでの距離（px）。小さいほど速度の上がり方が急になる")]
    [SerializeField] private float _lockOnDeadzone = 150f;
    [Tooltip("ターゲットがこの距離（m）を超えると自動でロックオン解除する")]
    [SerializeField] private float _autoUnlockRange = 25f;

    [Header("ロックオン開始ブレンド")]
    [Tooltip("ロックオン開始時のカメラ切り替えブレンドにかかる時間（秒）")]
    [SerializeField] private float _lockOnBlendDuration = 0.4f;
    [Tooltip("ブレンドのEaseOut強度。値が大きいほど最初の動きが速く、終わりに急激に収束する")]
    [SerializeField, Range(1f, 8f)] private float _lockOnBlendExponent = 3f;

    [SerializeField]
    private LockOnController _lockOnController;

    #endregion

    #region プライベートフィールド

    private Camera _mainCamera;
    private Transform _playerTransform;
    private ILockOnTarget _currentTarget;
    private CinemachineOrbitalFollow _normalOrbitalFollow;
    private CinemachineInputAxisController _normalInputAxisController;
    private Transform _cameraFollowTarget;
    private InputHandler _inputHandler;
    private Vector3 _normalFollowVelocity;
    private CameraShake _cameraShake;

    private bool _isLockOnBlending;
    private float _blendT;
    private Vector3 _blendStartPosition;
    private Quaternion _blendStartRotation;

    private float _timeScale = 1f;
    private float _basePositionSmoothTime;
    private Vector2 _baseRotationSpeed;
    private GameSettingService _gameSettingService;
    #endregion

    #region イージング関数

    /// <summary>最初速く、終わりにゆっくり収束する。ロックオン開始ブレンドに使用。</summary>
    private float EaseOut(float t) => 1f - Mathf.Pow(1f - t, _lockOnBlendExponent);

    #endregion

    #region Unityライフサイクル

    private void Awake()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        RefreshMainCamera(SceneManager.GetActiveScene());
        ServiceLocator.Register(this);

        // 設定変更を繰り返しても倍率が累積しないようInspector値を基準値として保持する。
        _basePositionSmoothTime = _posSmoothTime;
        _baseRotationSpeed = _cameraRotationSpeed;
        if (ServiceLocator.TryGet(out _gameSettingService))
        {
            ApplyGameSettings(_gameSettingService.CurrentSettings);
            _gameSettingService.OnSettingsChanged += ApplyGameSettings;
        }

        _cameraShake = new CameraShake();
        _cameraFollowTarget = new GameObject("CameraFollowTarget").transform;

        if (_normalCamera == null || _lockOnCamera == null)
        {
            Debug.LogError("[CameraManager] CinemachineCamera reference is missing.", this);
            return;
        }

        _normalCamera.Priority = _normalPriority;
        _lockOnCamera.Priority = _normalPriority - 1;

        _normalOrbitalFollow = _normalCamera.GetComponent<CinemachineOrbitalFollow>();
        _normalInputAxisController = _normalCamera.GetComponent<CinemachineInputAxisController>();
        if (_normalInputAxisController != null)
        {
            _normalInputAxisController.enabled = false;
        }
    }

    private void Start()
    {
        if (ServiceLocator.TryGet(out HitStopManager hitStopManager))
        {
            hitStopManager.Register(this, HitStopTargetGroup.Camera);
        }
    }

    private void FixedUpdate()
    {
        if (_playerTransform == null) return;
        if (Mathf.Approximately(TimeScale, 0f)) return;

        if (IsLockedOn)
            UpdateLockOnCamera();
        else
        {
            UpdateFreeCameraRotation();
            UpdateNormalCameraPosition();
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;

        if (_gameSettingService != null)
        {
            // シーン破棄後に設定変更イベントから呼ばれないよう購読を解除する。
            _gameSettingService.OnSettingsChanged -= ApplyGameSettings;
        }

        if (ServiceLocator.TryGet(out HitStopManager hitStopManager))
        {
            hitStopManager.Unregister(this, HitStopTargetGroup.Camera);
        }

        ServiceLocator.Unregister<CameraManager>();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode loadMode)
    {
        RefreshMainCamera(scene);
    }

    private void RefreshMainCamera(Scene scene)
    {
        if (scene.IsValid() && scene.isLoaded)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var camera in root.GetComponentsInChildren<Camera>(true))
                {
                    if (camera.CompareTag("MainCamera"))
                    {
                        _mainCamera = camera;
                        return;
                    }
                }
            }
        }

        _mainCamera = Camera.main;
    }

    private void ApplyGameSettings(GameSetting settings)
    {
        // 値0で低速、値1で高速になるよう、Inspector値を中央値として補正する。
        _posSmoothTime = _basePositionSmoothTime
            * Mathf.Lerp(2f, 0.5f, settings.CameraMoveSpeed);
        _cameraRotationSpeed = _baseRotationSpeed
            * Mathf.Lerp(0.25f, 2f, settings.CameraRotationSensitivity);
    }

    public void OnSpeedChange(float scale)
    {
        _timeScale = scale;
    }

    #endregion

    #region 通常カメラ

    /// <summary>
    /// 仮想アンカーをSmoothDampでプレイヤーに遅延追従させます。
    /// 通常カメラはこのアンカーをFollowするため、カメラに自然な遅れが生まれます。
    /// </summary>
    private void UpdateNormalCameraPosition()
    {
        _cameraFollowTarget.position = Vector3.SmoothDamp(
            _cameraFollowTarget.position,
            _playerTransform.position,
            ref _normalFollowVelocity,
            _posSmoothTime
        );
    }

    private void UpdateFreeCameraRotation()
    {
        if (_normalOrbitalFollow == null || _inputHandler == null) return;

        Vector2 input = _inputHandler.CameraMoveInput;
        if (input.sqrMagnitude <= 0.0001f) return;

        Vector2 rotationDelta = new(
            input.x * _cameraInputDirection.x * _cameraRotationSpeed.x,
            input.y * _cameraInputDirection.y * _cameraRotationSpeed.y
        );

        float deltaTime = Time.fixedDeltaTime * TimeScale;
        _normalOrbitalFollow.HorizontalAxis.Value += rotationDelta.x * deltaTime;
        _normalOrbitalFollow.VerticalAxis.Value =
            Mathf.Clamp(
                _normalOrbitalFollow.VerticalAxis.Value + rotationDelta.y * deltaTime,
                _normalOrbitalFollow.VerticalAxis.Range.x,
                _normalOrbitalFollow.VerticalAxis.Range.y);
    }

    #endregion

    #region ロックオンカメラ

    /// <summary>
    /// ロックオンカメラの更新。有効性・距離チェック後、ブレンド中か通常追従かで処理を分岐します。
    /// </summary>
    private void UpdateLockOnCamera()
    {
        if (!IsCurrentTargetValid())
        {
            Unlock();
            return;
        }

        if (IsTargetOutOfRange())
        {
            Unlock();
            return;
        }

        Transform targetCenter = _currentTarget.GetTargetCenter();

        if (_isLockOnBlending)
        {
            UpdateBlend(targetCenter);
            return;
        }

        UpdateLockOnCameraByArea(targetCenter);
        _cameraFollowTarget.position = _playerTransform.position;
    }

    /// <summary>
    /// ロックオン開始時のブレンドを更新します。
    /// EaseOutで位置・回転を同時に補間し、完了後は通常追従に移行します。
    /// </summary>
    private void UpdateBlend(Transform targetCenter)
    {
        _blendT += Time.fixedDeltaTime / _lockOnBlendDuration;
        float eased = EaseOut(Mathf.Clamp01(_blendT));

        Vector3 desiredPos = CalcDesiredPosition();
        Quaternion desiredRot = CalcDesiredRotation(targetCenter, desiredPos);

        _lockOnCamera.transform.position = Vector3.Lerp(_blendStartPosition, desiredPos, eased);
        _lockOnCamera.transform.rotation = Quaternion.Slerp(_blendStartRotation, desiredRot, eased);

        if (_blendT >= 1f) _isLockOnBlending = false;
    }

    /// <summary>
    /// カメラ位置と回転をエリア判定に基づいて更新します。
    /// </summary>
    private void UpdateLockOnCameraByArea(Transform targetCenter)
    {
        UpdateCameraPosition();
        UpdateCameraRotation(targetCenter);
    }

    /// <summary>
    /// カメラ位置を目標位置へ一定速度で追従させます。
    /// </summary>
    private void UpdateCameraPosition()
    {
        _lockOnCamera.transform.position = Vector3.MoveTowards(
            _lockOnCamera.transform.position,
            CalcDesiredPosition(),
            _lockOnPositionSpeed * Time.fixedDeltaTime
        );
    }

    /// <summary>
    /// ターゲットの画面上の位置に応じてカメラ回転を更新します。
    /// デッドゾーン内では回転せず、逸脱量に応じて速度が上がります。
    /// ターゲットがカメラ後方に回り込んだ場合は最大速度で強制追従します。
    /// </summary>
    private void UpdateCameraRotation(Transform targetCenter)
    {
        Vector3 rawScreenPos = _mainCamera.WorldToScreenPoint(targetCenter.position);

        if (rawScreenPos.z < 0)
        {
            RotateToward(targetCenter, _lockOnFollowSpeedMax);
            return;
        }

        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        float deviation = Vector2.Distance(new Vector2(rawScreenPos.x, rawScreenPos.y), screenCenter);

        if (deviation <= _lockOnAreaRadius) return;

        float deviationFromArea = deviation - _lockOnAreaRadius;
        float t = Mathf.Clamp01(deviationFromArea / _lockOnDeadzone);
        float speed = Mathf.Lerp(_lockOnFollowSpeedMin, _lockOnFollowSpeedMax, t);

        RotateToward(targetCenter, speed);
    }

    /// <summary>
    /// 指定速度でターゲット方向にカメラを回転させます。
    /// </summary>
    private void RotateToward(Transform targetCenter, float speed)
    {
        Quaternion targetRot = CalcDesiredRotation(targetCenter, _lockOnCamera.transform.position);
        _lockOnCamera.transform.rotation = Quaternion.Slerp(
            _lockOnCamera.transform.rotation,
            targetRot,
            Time.fixedDeltaTime * speed
        );
    }

    #endregion

    #region ヘルパー

    /// <summary>
    /// ロックオン開始時のブレンド初期値を記録します。
    /// </summary>
    private void BeginLockOnBlend()
    {
        _blendStartPosition = _lockOnCamera.transform.position;
        _blendStartRotation = _lockOnCamera.transform.rotation;
        _blendT = 0f;
        _isLockOnBlending = true;
    }

    ///  <summary>
    /// プレイヤーの真後ろにカメラを置くための目標位置を計算します。
    /// </summary>
    private Vector3 CalcDesiredPosition()
    {
        // ロックオン中はカメラの向きの逆方向から距離を取る
        if (IsLockedOn)
        {
            Vector3 back = -_lockOnCamera.transform.forward;
            back.y = 0f;
            back.Normalize();

            return _playerTransform.position
                 + (back * _cameraDistance)
                 + (Vector3.up * _cameraHeight);
        }

        // 通常時はプレイヤーの向きの逆方向
        Vector3 backNormal = -_playerTransform.forward;
        backNormal.y = 0f;
        backNormal.Normalize();

        return _playerTransform.position
             + (backNormal * _cameraDistance)
             + (Vector3.up * _cameraHeight);
    }

    /// <summary>
    /// プレイヤーとターゲットの中間点を見るカメラ回転を計算します。
    /// </summary>
    private Quaternion CalcDesiredRotation(Transform targetCenter, Vector3 fromPosition)
    {
        Vector3 lookAtPoint = Vector3.Lerp(_playerTransform.position, targetCenter.position, 0.5f);
        Vector3 dir = lookAtPoint - fromPosition;
        if (dir.sqrMagnitude < 0.001f) return _lockOnCamera.transform.rotation;
        return Quaternion.LookRotation(dir);
    }

    /// <summary>
    /// ロックオン解除時に現在のカメラ角度を通常カメラのOrbitalFollowに引き継ぎます。
    /// </summary>
    private void ApplyRotationToNormalCamera()
    {
        if (_normalOrbitalFollow == null) return;

        Vector3 euler = _lockOnCamera.transform.rotation.eulerAngles;
        _normalOrbitalFollow.HorizontalAxis.Value = euler.y;
        _normalOrbitalFollow.VerticalAxis.Value = euler.x;
    }

    /// <summary>
    /// ターゲットが有効なロックオン対象かチェックします。
    /// </summary>
    private bool IsValidTarget(ILockOnTarget target)
    {
        if (!target.IsLockable || target.GetTargetCenter() == null)
        {
            Debug.LogWarning("ロックオン対象が無効です。");
            return false;
        }
        return true;
    }

    /// <summary>
    /// 現在のターゲットが有効な状態かチェックします。
    /// </summary>
    private bool IsCurrentTargetValid()
    {
        return _currentTarget.IsLockable
            && _currentTarget.GetTargetCenter() != null;
    }

    /// <summary>
    /// 現在のターゲットが自動解除距離を超えているかチェックします。
    /// </summary>
    private bool IsTargetOutOfRange()
    {
        return Vector3.Distance(_playerTransform.position, _currentTarget.GetTargetCenter().position) > _autoUnlockRange;
    }

    #endregion
}
