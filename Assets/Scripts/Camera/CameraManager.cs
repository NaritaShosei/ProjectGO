using System;
using Cysharp.Threading.Tasks;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// カメラの挙動を管理するクラス。
/// 通常時の追従遅延およびロックオン時のターゲット追従を制御します。
/// ターゲット選定はLockOnControllerに、演出（ズーム・カメラシェイク）の発火は
/// CameraPresentationControllerに委譲しています。
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

    /// <summary>現在のズームFOV倍率。1で通常視野、1未満でズームイン、1より大きい値でズームアウト。</summary>
    public float CurrentZoom => _cameraPresentationController?.CurrentZoom ?? 1f;

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

        _occlusionTransparencyController?.Dispose();
        _occlusionTransparencyController = new CameraOcclusionTransparencyController(
            _playerTransform,
            MainCamera,
            _occlusionMask,
            _occlusionCastRadius,
            _occludedAlpha,
            _occlusionFadeSpeed);

        if (ServiceLocator.TryGet(out HitStopManager hitStopManager))
        {
            hitStopManager.Register(this, HitStopTargetGroup.Camera);
        }

        _cameraMotionController = new CameraMotionController(
            new CameraReferences(
                _normalCamera,
                _lockOnCamera,
                _normalOrbitalFollow,
                _normalInputAxisController,
                _playerTransform),
            new NormalCameraSettings(
                _cameraInputDirection,
                _posSmoothTime,
                _cameraRotationSpeed),
            new LockOnSettings(
                _cameraDistance,
                _cameraHeight,
                _lockOnAreaRadius,
                _lockOnPositionSpeed,
                _lockOnFollowSpeedMin,
                _lockOnFollowSpeedMax,
                _lockOnDeadzone),
            new LockOnBlendSettings(
                _lockOnBlendDuration,
                _lockOnBlendExponent,
                _lockOnBlendMaxAngularSpeed,
                _lockOnBlendMaxLinearSpeed,
                _lockOnBlendMaxExtraTime));

        if (ServiceLocator.TryGet(out InputHandler inputHandler) && ServiceLocator.TryGet(out EnemyManager enemyManager))
        {
            _lockOnController.Init(this, inputHandler, enemyManager, _playerTransform, _cameraMotionController);
            _lockOnController.OnTargetChanged += HandleTargetChanged;
        }
        else
        {
            Debug.LogError("[CameraManager] InputHandler or EnemyManager is missing. LockOn is disabled.", this);
        }

        _cameraPresentationController = new CameraPresentationController(
            _normalCamera,
            _lockOnCamera,
            player.GetComponent<PlayerAttack>(),
            player.GetComponent<PlayerModeController>(),
            player.GetComponentInChildren<PlayerAnimationController>(),
            _level2Zoom,
            _level3Zoom,
            _releaseZoom,
            _thunderModeZoom);
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

    /// <summary>
    /// ズームのFOV倍率を設定します。1は変化なし、1未満でズームイン、1より大きい値でズームアウトです。
    /// 現在値からの移動距離に関わらず、必ずduration秒かけて到達します。
    /// </summary>
    public void SetZoom(float zoom, float duration)
    {
        _cameraPresentationController?.SetZoom(zoom, duration);
    }

    /// <summary>
    /// チャージ段階をFOV倍率へ変換して設定します。
    /// 各段階の倍率・到達時間はInspectorの「ズーム設定」で個別に調整できます。
    /// Level1はズームなしのため何もしません。
    /// </summary>
    /// <returns>実際にズーム値を変更した場合はtrue。</returns>
    public bool SetZoomLevel(ChargeLevel level)
    {
        return _cameraPresentationController != null && _cameraPresentationController.SetZoomLevel(level);
    }

    /// <summary>指定量だけズームインします（倍率を下げます）。</summary>
    public void ZoomIn(float amount, float duration)
    {
        _cameraPresentationController?.ZoomIn(amount, duration);
    }

    /// <summary>指定量だけズームアウトします（倍率を上げます）。</summary>
    public void ZoomOut(float amount, float duration)
    {
        _cameraPresentationController?.ZoomOut(amount, duration);
    }

    /// <summary>ズームを通常視野（倍率1.0）へ戻します。</summary>
    public void ResetZoom(float duration = 0f)
    {
        _cameraPresentationController?.ResetZoom(duration);
    }

    /// <summary>カメラシェイクを実行します。</summary>
    public async UniTask ExecutionCameraShake(CameraShakeData data)
    {
        if (_cameraPresentationController == null) return;

        var camera = IsLockedOn ? _lockOnCamera : _normalCamera;
        await _cameraPresentationController.Shake(camera, data);
    }

    /// <summary>カメラシェイクを強制停止します。</summary>
    public void ExecutionForceStopCameraShake()
    {
        _cameraPresentationController?.ForceStopShake();
    }

    #endregion

    #region Inspectorフィールド

    [Header("カメラ参照")]
    [SerializeField] private Camera _mainCamera;
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

    [Header("カメラ近接エフェクト非表示設定")]
    [Tooltip("カメラへ近づいた際に非表示にするエフェクト。対象は後からInspectorで指定できます")]
    [SerializeField] private Transform[] _cameraProximityEffects = { };
    [Tooltip("カメラを球として扱う際の半径（m）")]
    [SerializeField, Min(0f)] private float _effectCameraRadius = 1.5f;
    [Tooltip("カメラ球の外側からエフェクトを非表示にし始める距離（m）")]
    [SerializeField, Min(0f)] private float _effectHideStartDistance = 1f;
    [Tooltip("選択中にカメラ近接エフェクトの判定範囲をSceneビューへ表示する")]
    [SerializeField] private bool _showEffectProximityGizmos = true;

    [Header("遮蔽物透過設定")]
    [Tooltip("透過対象として検出するLayer")]
    [SerializeField] private LayerMask _occlusionMask = ~0;
    [Tooltip("カメラとプレイヤーを結ぶ判定の太さ（m）")]
    [SerializeField, Min(0f)] private float _occlusionCastRadius = 0.25f;
    [Tooltip("遮蔽中の不透明度")]
    [SerializeField, Range(0f, 1f)] private float _occludedAlpha = 0.25f;
    [Tooltip("1秒あたりの不透明度変化量")]
    [SerializeField, Min(0.01f)] private float _occlusionFadeSpeed = 5f;

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
    [Tooltip("ロックオン開始ブレンドの基準時間（秒）")]
    [SerializeField] private float _lockOnBlendDuration = 0.4f;
    [Tooltip("ブレンドのEaseOut強度。大きいほど序盤が速い")]
    [SerializeField, Range(1f, 8f)] private float _lockOnBlendExponent = 3f;
    [Tooltip("ブレンド中の回転の最大角速度（度/秒）。大きくズレたときだけ効く")]
    [SerializeField] private float _lockOnBlendMaxAngularSpeed = 240f;
    [Tooltip("ブレンド中の位置の最大移動速度（m/秒）。大きくズレたときだけ効く")]
    [SerializeField] private float _lockOnBlendMaxLinearSpeed = 25f;
    [Tooltip("速度上限で基準時間内に追いつかない場合の追加許容時間（秒）。超えたら強制終了")]
    [SerializeField] private float _lockOnBlendMaxExtraTime = 0.6f;

    [Header("ズーム設定")]
    [Tooltip("チャージ段階Level2時のFOV倍率と到達時間（Level1はズームなし固定）")]
    [SerializeField] private ChargeZoomSetting _level2Zoom = new() { Multiplier = 0.85f, Duration = 0.3f };
    [Tooltip("チャージ段階Level3時のFOV倍率と到達時間")]
    [SerializeField] private ChargeZoomSetting _level3Zoom = new() { Multiplier = 0.7f, Duration = 0.25f };
    [Tooltip("チャージ解放（攻撃発動 or キャンセル）時のオーバーシュート倍率・到達時間・通常視野へ戻るまでの時間")]
    [SerializeField] private ReleaseZoomSetting _releaseZoom = new() { OvershootMultiplier = 1.1f, OvershootDuration = 0.15f, SettleDuration = 0.35f };
    [Tooltip("雷神モードへ切り替わった瞬間のズームイン倍率・到達時間・通常視野へ戻るまでの時間")]
    [SerializeField] private ModeChangeZoomSetting _thunderModeZoom = new() { Multiplier = 0.8f, ZoomInDuration = 0.15f, MidMultiplier = 0.8f, MidDuration = 0.1f, ZoomOutDuration = 0.3f };

    [SerializeField]
    private LockOnController _lockOnController;

    #endregion

    #region プライベートフィールド

    private Transform _playerTransform;

    private CinemachineOrbitalFollow _normalOrbitalFollow;
    private CinemachineInputAxisController _normalInputAxisController;
    private CameraMotionController _cameraMotionController;
    private CameraPresentationController _cameraPresentationController;
    private EffectCameraProximityController _effectCameraProximityController;
    private CameraOcclusionTransparencyController _occlusionTransparencyController;

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

        _effectCameraProximityController = new EffectCameraProximityController(
            MainCamera,
            _cameraProximityEffects);

        // 設定変更を繰り返しても倍率が累積しないようInspector値を基準値として保持する。
        _basePositionSmoothTime = _posSmoothTime;
        _baseRotationSpeed = _cameraRotationSpeed;
        if (ServiceLocator.TryGet(out _gameSettingService))
        {
            ApplyGameSettings(_gameSettingService.CurrentSettings);
            _gameSettingService.OnSettingsChanged += ApplyGameSettings;
        }

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

        _cameraPresentationController?.Tick(Time.fixedDeltaTime * TimeScale);
        _lockOnController?.Tick(TimeScale);
    }

    private void LateUpdate()
    {
        _effectCameraProximityController?.UpdateEffects(
            _effectCameraRadius,
            _effectHideStartDistance);
        _occlusionTransparencyController?.UpdateTransparency(Time.deltaTime);
    }

    private void OnDrawGizmosSelected()
    {
        if (!_showEffectProximityGizmos) return;

        Camera targetCamera = _mainCamera != null ? _mainCamera : Camera.main;
        if (targetCamera == null) return;

        Vector3 cameraPosition = targetCamera.transform.position;
        float cameraRadius = Mathf.Max(0f, _effectCameraRadius);
        float hideDistance = cameraRadius + Mathf.Max(0f, _effectHideStartDistance);

        // 黄色はカメラ自体の大きさ、赤色はエフェクトが非アクティブになる実距離を表す。
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(cameraPosition, cameraRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(cameraPosition, hideDistance);

        if (_cameraProximityEffects == null) return;

        Gizmos.color = Color.cyan;
        foreach (Transform effectTransform in _cameraProximityEffects)
        {
            if (effectTransform == null) continue;
            Gizmos.DrawLine(cameraPosition, effectTransform.position);
            Gizmos.DrawWireSphere(effectTransform.position, 0.15f);
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;

        if (_lockOnController != null)
        {
            _lockOnController.OnTargetChanged -= HandleTargetChanged;
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
        _cameraPresentationController?.ResetZoom();
        _cameraPresentationController?.Dispose();
        _effectCameraProximityController?.Dispose();
        _occlusionTransparencyController?.Dispose();

        ServiceLocator.Unregister<CameraManager>();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode loadMode)
    {
        RefreshMainCamera(scene);
        _lockOnController?.SetMainCamera(_mainCamera);
        _effectCameraProximityController?.SetMainCamera(_mainCamera);
        _occlusionTransparencyController?.SetMainCamera(_mainCamera);
    }

    private void RefreshMainCamera(Scene scene)
    {
        if (_mainCamera != null && _mainCamera.gameObject.scene == scene)
        {
            return;
        }

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
