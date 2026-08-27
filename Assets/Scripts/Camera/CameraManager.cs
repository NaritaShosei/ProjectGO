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
    public ILockOnTarget CurrentTarget => _lockOnController?.CurrentTarget;

    /// <summary>現在ロックオン中かどうか</summary>
    public bool IsLockedOn => _lockOnController != null && _lockOnController.IsLockedOn;

    public float TimeScale => _timeScale;

    /// <summary>ロックオンエリアの半径（px）</summary>
    public float LockOnAreaRadius => _lockOnAreaRadius;

    /// <summary>ロックオンを自動解除する距離を取得します。</summary>
    public float AutoUnlockRange => _autoUnlockRange;

    /// <summary>現在のズーム率。0は通常視野、1は最大ズーム。</summary>
    public float CurrentZoom => _cameraZoomController?.CurrentZoom ?? 0f;

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

        if (_normalCamera == null || _lockOnCamera == null || _lockOnController == null)
        {
            Debug.LogError("[CameraManager] Required camera references are missing.", this);
            return;
        }

        _playerTransform = player.transform;

        if (ServiceLocator.TryGet(out HitStopManager hitStopManager))
        {
            hitStopManager.Register(this, HitStopTargetGroup.Camera);
        }

        _cameraMotionController = new CameraMotionController(
            _normalCamera,
            _lockOnCamera,
            _normalOrbitalFollow,
            _normalInputAxisController,
            _playerTransform,
            _cameraInputDirection,
            _cameraDistance,
            _cameraHeight,
            _posSmoothTime,
            _cameraRotationSpeed,
            _lockOnAreaRadius,
            _lockOnPositionSpeed,
            _lockOnFollowSpeedMin,
            _lockOnFollowSpeedMax,
            _lockOnDeadzone,
            _lockOnBlendDuration,
            _lockOnBlendExponent);

        if (ServiceLocator.TryGet(out InputHandler inputHandler) && ServiceLocator.TryGet(out EnemyManager enemyManager))
        {
            _lockOnController.Init(this, inputHandler, enemyManager, _playerTransform, _cameraMotionController);
            _lockOnController.OnTargetChanged += HandleTargetChanged;
        }
        else
        {
            Debug.LogError("[CameraManager] InputHandler or EnemyManager is missing. LockOn is disabled.", this);
        }

        _playerAttack = player.GetComponent<PlayerAttack>();
        if (_playerAttack != null)
        {
            _playerAttack.OnChargeLevelReached += HandleChargeLevelReached;
            _playerAttack.OnChargingEnded += HandleChargingEnded;
        }
    }

    /// <summary>
    /// 指定したターゲットをロックオンします。
    /// ロックオン開始時はEaseOutブレンドで滑らかにカメラを切り替えます。
    /// </summary>
    public void LockOn(ILockOnTarget target)
    {
        _lockOnController?.LockOn(target);
    }

    /// <summary>
    /// ロックオンを解除し、通常カメラに戻します。
    /// 解除時は現在のカメラ角度を通常カメラに引き継ぎます。
    /// </summary>
    public void Unlock()
    {
        _lockOnController?.Unlock();
    }

    /// <summary>ズーム率を設定します。0は通常視野、1は最大ズームです。</summary>
    public void SetZoom(float zoom)
    {
        _cameraZoomController?.SetZoom(zoom);
    }

    /// <summary>
    /// チャージ段階をズーム率へ変換して設定します。
    /// 各段階のズーム率はInspectorの「ズーム設定」で個別に調整できます。
    /// </summary>
    public void SetZoomLevel(ChargeLevel level)
    {
        float zoom = level switch
        {
            ChargeLevel.Level1 => _chargeZoomLevel1,
            ChargeLevel.Level2 => _chargeZoomLevel2,
            ChargeLevel.Level3 => _chargeZoomLevel3,
            _ => 0f,
        };
        SetZoom(zoom);
    }

    /// <summary>指定量だけズームインします。</summary>
    public void ZoomIn(float amount)
    {
        _cameraZoomController?.ZoomIn(amount);
    }

    /// <summary>指定量だけズームアウトします。</summary>
    public void ZoomOut(float amount)
    {
        _cameraZoomController?.ZoomOut(amount);
    }

    /// <summary>ズームを通常視野へ戻します。</summary>
    public void ResetZoom()
    {
        _cameraZoomController?.ResetZoom();
    }

    /// <summary>チャージ段階の通知を受けてズーム率を変更します。</summary>
    private void HandleChargeLevelReached(ChargeLevel level)
    {
        Debug.Log($"[CameraManager] ChargeLevel : {level}");
        SetZoomLevel(level);
    }

    /// <summary>
    /// チャージ解放（攻撃発動 or キャンセル）を受けて、解放時点のズーム率から
    /// Inspectorで指定したズーム率まで少しズームアウトします。
    /// </summary>
    private void HandleChargingEnded()
    {
        SetZoom(_chargeReleaseZoom);
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

    [Header("ズーム設定")]
    [Tooltip("ズーム率1のときに使用するField of View")]
    [SerializeField] private float _minFieldOfView = 30f;
    [Tooltip("1秒あたりに変化するズーム率")]
    [SerializeField] private float _zoomSpeed = 3f;
    [Tooltip("チャージ段階Level1時のズーム率（0で通常視野、1で最大ズーム）")]
    [SerializeField, Range(0f, 1f)] private float _chargeZoomLevel1 = 1f / 3f;
    [Tooltip("チャージ段階Level2時のズーム率（0で通常視野、1で最大ズーム）")]
    [SerializeField, Range(0f, 1f)] private float _chargeZoomLevel2 = 2f / 3f;
    [Tooltip("チャージ段階Level3時のズーム率（0で通常視野、1で最大ズーム）")]
    [SerializeField, Range(0f, 1f)] private float _chargeZoomLevel3 = 1f;
    [Tooltip("チャージ解放（攻撃発動 or キャンセル）時にズームアウトする先のズーム率（0で通常視野）")]
    [SerializeField, Range(0f, 1f)] private float _chargeReleaseZoom = 0f;

    [SerializeField]
    private LockOnController _lockOnController;

    #endregion

    #region プライベートフィールド

    private Camera _mainCamera;
    private Transform _playerTransform;
    private CameraShake _cameraShake;

    private CinemachineOrbitalFollow _normalOrbitalFollow;
    private CinemachineInputAxisController _normalInputAxisController;
    private CameraMotionController _cameraMotionController;
    private CameraZoomController _cameraZoomController;
    private PlayerAttack _playerAttack;

    private float _timeScale = 1f;
    private float _basePositionSmoothTime;
    private Vector2 _baseRotationSpeed;
    private GameSettingService _gameSettingService;
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

        _cameraZoomController = new CameraZoomController(
            _normalCamera,
            _lockOnCamera,
            _minFieldOfView,
            _zoomSpeed);
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

        _cameraZoomController?.Tick(Time.fixedDeltaTime * TimeScale);
        _lockOnController?.Tick(TimeScale);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;

        if (_lockOnController != null)
        {
            _lockOnController.OnTargetChanged -= HandleTargetChanged;
        }

        if (_playerAttack != null)
        {
            _playerAttack.OnChargeLevelReached -= HandleChargeLevelReached;
            _playerAttack.OnChargingEnded -= HandleChargingEnded;
        }

        if (_gameSettingService != null)
        {
            // シーン破棄後に設定変更イベントから呼ばれないよう購読を解除する。
            _gameSettingService.OnSettingsChanged -= ApplyGameSettings;
        }

        if (ServiceLocator.TryGet(out HitStopManager hitStopManager))
        {
            hitStopManager.Unregister(this, HitStopTargetGroup.Camera);
        }

        _cameraMotionController?.Dispose();
        _cameraZoomController?.ResetZoom();

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
        _cameraMotionController?.SetNormalSettings(_posSmoothTime, _cameraRotationSpeed);
    }

    public void OnSpeedChange(float scale)
    {
        _timeScale = scale;
    }

    #endregion

    internal void SetLockOnCameraActive(bool isActive)
    {
        _normalCamera.Priority = isActive ? _normalPriority - 1 : _normalPriority;
        _lockOnCamera.Priority = isActive ? _lockOnPriority : _normalPriority - 1;
    }

    private void HandleTargetChanged(ILockOnTarget target)
    {
        OnLockOnTargetChanged?.Invoke(target);
    }
}
