using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// カメラ入力とロックオン状態の遷移を管理するControllerです。
/// ターゲット選択はLockOnTargetSelectorに、カメラの動きは各Stateに委譲します。
/// 対象切り替えの入力は InputHandler を介さず Gamepad.current / Mouse.current を直接参照します
/// （変更をCameraフォルダ内に閉じるための割り切り）。
/// </summary>
public class CameraController : MonoBehaviour
{
    #region パブリックプロパティ・イベント

    /// <summary>現在ロックオンしている対象を取得します。</summary>
    public ILockOnTarget CurrentTarget => _lockOnState?.Target;

    /// <summary>現在ロックオン中か取得します。</summary>
    public bool IsLockedOn => _currentState == _lockOnState;

    /// <summary>ロックオン対象が変更されたときに通知します。</summary>
    public event System.Action<ILockOnTarget> OnTargetChanged;

    #endregion

    #region 初期化

    /// <summary>
    /// カメラControllerを初期化し、入力と敵イベントを購読します。
    /// </summary>
    public void Init(
        CameraManager cameraManager,
        InputHandler inputHandler,
        EnemyManager enemyManager,
        Transform playerTransform,
        CameraMotionController motionController)
    {
        if (cameraManager == null || inputHandler == null || enemyManager == null || playerTransform == null)
        {
            Debug.LogError("[LockOnController] Init arguments are missing.", this);
            return;
        }

        _cameraManager = cameraManager;
        _inputHandler = inputHandler;
        _motionController = motionController;

        _normalState = new NormalCameraState(_motionController);
        _lockOnState = new LockOnCameraState(
            _motionController,
            _cameraManager.MainCamera,
            playerTransform,
            _cameraManager.AutoUnlockRange);
        _currentState = _normalState;
        _currentState.Enter();

        _selector = new LockOnTargetSelector(
            playerTransform,
            _lockOnRange,
            enemyManager,
            _cameraManager.MainCamera
        );

        SubscribeInputEvents();

        // 敵の強制削除時の次ターゲット自動選択（通常の撃破は Tick の有効性チェックで拾う）
        enemyManager.OnEnemyForceRemoved += HandleEnemyForceRemoved;
    }

    /// <summary>現在のカメラ状態を更新します。</summary>
    public void Tick(float timeScale)
    {
        if (_currentState == null) return;

        if (_currentState == _lockOnState && TryHandleInvalidTarget())
        {
            return;
        }

        UpdateTargetSwitch(timeScale);
        _currentState.Tick(timeScale, _inputHandler.CameraMoveInput);
    }

    /// <summary>
    /// ロックオン対象が無効になっていないか確認する。
    /// 自動解除距離を超えた → 解除。撃破・削除・非ロック化 → 次の対象へ、いなければ解除。
    /// </summary>
    /// <returns>解除または切り替えを行った場合 true（このフレームの以降の更新はスキップ）。</returns>
    private bool TryHandleInvalidTarget()
    {
        if (_lockOnState.IsTargetOutOfRange)
        {
            Unlock();
            return true;
        }

        if (!_lockOnState.IsTargetValid)
        {
            var next = _selector.SelectNextTarget(_lockOnState.Target);
            if (next != null) LockOn(next);
            else Unlock();
            return true;
        }

        return false;
    }

    /// <summary>ロックオン処理で使用するメインカメラを更新します。</summary>
    public void SetMainCamera(Camera mainCamera)
    {
        if (mainCamera == null) return;

        _lockOnState?.SetMainCamera(mainCamera);
        _selector?.SetMainCamera(mainCamera);
    }

    /// <summary>指定した対象へロックオンします。</summary>
    public void LockOn(ILockOnTarget target)
    {
        if (target == null || !target.IsLockable || target.GetTargetCenter() == null)
            return;

        if (CurrentTarget == target) return;

        bool wasLockedOn = IsLockedOn;

        if (_currentState != _normalState)
        {
            _currentState.Exit();
        }

        // 初回か対象切り替えかでブレンド起点が変わる
        _lockOnState.SetTarget(target, isInitialLockOn: !wasLockedOn);
        _currentState = _lockOnState;
        _cameraManager.SetLockOnCameraActive(true);
        _currentState.Enter();

        // 初回のみ、ロックオン前に溜まった切り替え入力を捨てる
        if (!wasLockedOn) ResetSwitchAccumulators();

        OnTargetChanged?.Invoke(target);
    }

    /// <summary>ロックオンを解除して通常状態へ戻します。</summary>
    public void Unlock()
    {
        if (!IsLockedOn) return;

        _currentState.Exit();
        _currentState = _normalState;
        _cameraManager.SetLockOnCameraActive(false);
        _currentState.Enter();
        OnTargetChanged?.Invoke(null);
    }

    #endregion

    #region プライベートフィールド

    [Tooltip("ロックオン可能な最大距離（m）")]
    [SerializeField] private float _lockOnRange = 20f;

    [Header("対象切り替え（スティック）")]
    [Tooltip("倒し量×時間 の蓄積がこの値を超えると1回切り替える")]
    [SerializeField] private float _switchStickThreshold = 0.35f;
    [Tooltip("スティックの横成分がこの絶対値未満のときは蓄積しない（デッドゾーン）")]
    [SerializeField, Range(0f, 1f)] private float _switchStickDeadzone = 0.2f;

    [Header("対象切り替え（マウス）")]
    [Tooltip("マウス横移動量の累積がこの絶対値を超えると1回切り替える")]
    [SerializeField] private float _switchMouseThreshold = 400f;

    private CameraManager _cameraManager;
    private InputHandler _inputHandler;
    private LockOnTargetSelector _selector;
    private CameraMotionController _motionController;
    private NormalCameraState _normalState;
    private LockOnCameraState _lockOnState;
    private ICameraState _currentState;

    // 対象切り替えの入力蓄積（符号付き。正で右、負で左）
    private float _switchAccumStick;
    private float _switchAccumMouse;
    // マウス横移動量を Update でフレーム精度で貯め、Tick で消費する
    private float _mouseSwitchDeltaX;

    #endregion

    #region プライベートメソッド

    private void SubscribeInputEvents()
    {
        _inputHandler.OnLockOn += HandleLockOnInput;
    }

    #region Unityライフサイクル

    private void Update()
    {
        // マウス切り替え用の横移動量をフレーム精度で貯める（FixedUpdateだと取りこぼすため）
        if (IsLockedOn && Mouse.current != null)
        {
            _mouseSwitchDeltaX += Mouse.current.delta.ReadValue().x;
        }
    }

    private void OnDestroy()
    {
        if (_inputHandler != null)
        {
            _inputHandler.OnLockOn -= HandleLockOnInput;
        }

        if (ServiceLocator.TryGet(out EnemyManager enemyManager))
        {
            enemyManager.OnEnemyForceRemoved -= HandleEnemyForceRemoved;
        }
    }

    #endregion

    #region 入力ハンドラ

    /// <summary>
    /// ロックオンボタン入力。
    /// ロックオン中は解除、未ロックオン時は手動ロックオン開始。
    /// </summary>
    private void HandleLockOnInput()
    {
        if (_cameraManager == null || _selector == null) return;

        if (IsLockedOn)
        {
            // 自動ロックオン中に手動ロックオンボタンを押した場合も解除
            Unlock();
        }
        else
        {
            TryManualLockOn();
        }
    }
    /// <summary>切り替え入力を蓄積し、閾値を超えたらその方向のターゲットへ切り替える。</summary>
    private void UpdateTargetSwitch(float timeScale)
    {
        if (_inputHandler == null || _selector == null) return;
        if (!IsLockedOn) return;

        // スティック：デッドゾーン超えの間だけ「倒し量×時間」を蓄積
        float stickX = Gamepad.current != null ? Gamepad.current.rightStick.ReadValue().x : 0f;
        if (Mathf.Abs(stickX) >= _switchStickDeadzone)
        {
            // 逆方向へ倒したら蓄積をリセット
            if (_switchAccumStick != 0f && Mathf.Sign(stickX) != Mathf.Sign(_switchAccumStick))
                _switchAccumStick = 0f;

            _switchAccumStick += stickX * (Time.fixedDeltaTime * timeScale);

            // 閾値到達で切り替え、蓄積を0へ（成否に関わらず）
            if (Mathf.Abs(_switchAccumStick) >= _switchStickThreshold)
            {
                TrySwitchTarget(Mathf.Sign(_switchAccumStick));
                _switchAccumStick = 0f;
            }
        }

        // マウス：Update で貯めた横移動量を消費して符号付きで累積（時間は掛けない）
        float mouseDelta = _mouseSwitchDeltaX;
        _mouseSwitchDeltaX = 0f;
        if (mouseDelta != 0f)
        {
            // 逆方向へ動かしたら蓄積をリセット
            if (_switchAccumMouse != 0f && Mathf.Sign(mouseDelta) != Mathf.Sign(_switchAccumMouse))
                _switchAccumMouse = 0f;

            _switchAccumMouse += mouseDelta;

            // 閾値到達で切り替え、蓄積を0へ（成否に関わらず）
            if (Mathf.Abs(_switchAccumMouse) >= _switchMouseThreshold)
            {
                TrySwitchTarget(Mathf.Sign(_switchAccumMouse));
                _switchAccumMouse = 0f;
            }
        }
    }

    /// <summary>指定方向のターゲットへ切り替える。対象がいなければ何もしない。</summary>
    private void TrySwitchTarget(float direction)
    {
        var next = _selector.SelectSwitchTarget(CurrentTarget, direction);
        if (next != null) LockOn(next);
    }

    /// <summary>対象切り替えの入力蓄積を0に戻す。ロックオン開始時に呼ぶ。</summary>
    private void ResetSwitchAccumulators()
    {
        _switchAccumStick = 0f;
        _switchAccumMouse = 0f;
        _mouseSwitchDeltaX = 0f;
    }

    #endregion

    #region ロックオン操作

    /// <summary>
    /// 手動ロックオンを試みます。
    /// 対象が見つからなければ何もしません。
    /// </summary>
    private void TryManualLockOn()
    {
        if (_cameraManager == null || _selector == null) return;

        var target = _selector.SelectInitialTarget();
        if (target == null) return;

        LockOn(target);
    }

    #endregion

    #region 敵撃破ハンドラ

    /// <summary>敵の強制削除時の処理。削除されたのが現在の対象なら次へ切り替え、なければ解除する。</summary>
    private void HandleEnemyForceRemoved(IEnemy removedEnemy)
    {
        if (_cameraManager == null || _selector == null) return;
        if (!IsLockedOn) return;
        if (CurrentTarget != removedEnemy) return;

        var next = _selector.SelectNextTarget(removedEnemy);
        if (next != null)
        {
            LockOn(next);
        }
        else
        {
            Unlock();
        }
    }

    #endregion
    #endregion
}
