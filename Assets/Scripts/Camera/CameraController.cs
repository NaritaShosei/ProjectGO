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
            _cameraManager.MainCamera,
            new LockOnScoreWeights(
                _scoreWeightScreenCenter,
                _scoreWeightPlayerDistance,
                _scoreWeightCameraSide,
                _scoreCenterAngleReference)
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

        UpdateTargetSwitch();
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
        if (!wasLockedOn) ResetSwitchState();

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

    [Header("ロックオン優先度スコアの重み（小さいほど優先）")]
    [Tooltip("画面中心からのズレの重み")]
    [SerializeField] private float _scoreWeightScreenCenter = 1f;
    [Tooltip("プレイヤーからの距離の重み")]
    [SerializeField] private float _scoreWeightPlayerDistance = 0.5f;
    [Tooltip("プレイヤーより手前（カメラ側）にいる敵へのペナルティの重み")]
    [SerializeField] private float _scoreWeightCameraSide = 1f;
    [Tooltip("画面中心ズレを 0..1 に正規化する基準角（度）。この角度でスコア1.0")]
    [SerializeField] private float _scoreCenterAngleReference = 50f;

    [Header("対象切り替え（スティック）")]
    [Tooltip("右スティック横成分がこの絶対値を超えたら1回切り替える")]
    [SerializeField, Range(0f, 1f)] private float _switchStickOnThreshold = 0.6f;
    [Tooltip("右スティック横成分がこの絶対値以下に戻ると次の切り替えを許可する（ヒステリシス）")]
    [SerializeField, Range(0f, 1f)] private float _switchStickOffThreshold = 0.3f;

    [Header("対象切り替え（マウス）")]
    [Tooltip("1回の連続した横スワイプの移動量がこの絶対値を超えたら1回切り替える")]
    [SerializeField] private float _switchMouseThreshold = 400f;
    [Tooltip("1 FixedUpdate のマウス横移動がこのpx未満ならスワイプ終了とみなす")]
    [SerializeField] private float _switchMouseMinStep = 6f;

    private CameraManager _cameraManager;
    private InputHandler _inputHandler;
    private LockOnTargetSelector _selector;
    private CameraMotionController _motionController;
    private NormalCameraState _normalState;
    private LockOnCameraState _lockOnState;
    private ICameraState _currentState;

    // 1入力につき1回だけ切り替えるためのラッチ。ニュートラル復帰／スワイプ終了で再武装する
    private bool _stickSwitchArmed;
    private bool _mouseSwitchArmed;
    // 進行中の横スワイプの移動量（符号付き。正で右、負で左）
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
    /// <summary>切り替え入力を判定する。1入力（1プッシュ／1スワイプ）につき1回だけ切り替える。</summary>
    private void UpdateTargetSwitch()
    {
        if (_selector == null || !IsLockedOn) return;

        UpdateStickSwitch();
        UpdateMouseSwitch();
    }

    /// <summary>右スティック：オン閾値を超えたら1回切り替え。オフ閾値以下に戻るまで再切り替えしない。</summary>
    private void UpdateStickSwitch()
    {
        float stickX = Gamepad.current != null ? Gamepad.current.rightStick.ReadValue().x : 0f;
        float absX = Mathf.Abs(stickX);

        // ニュートラル付近まで戻ったら次の切り替えを許可
        if (absX <= _switchStickOffThreshold) _stickSwitchArmed = true;

        // 武装中にオン閾値を超えたら1回だけ切り替え
        if (_stickSwitchArmed && absX >= _switchStickOnThreshold)
        {
            TrySwitchTarget(Mathf.Sign(stickX));
            _stickSwitchArmed = false;
        }
    }

    /// <summary>マウス：連続した横スワイプの移動量が閾値を超えたら1回切り替え。スワイプ終了／反転まで再切り替えしない。</summary>
    private void UpdateMouseSwitch()
    {
        // Update で貯めた1 Tick分の横移動量を取り出す
        float delta = _mouseSwitchDeltaX;
        _mouseSwitchDeltaX = 0f;

        // 移動が小さい Tick はスワイプ終了とみなし、蓄積を捨てて再武装
        if (Mathf.Abs(delta) < _switchMouseMinStep)
        {
            _switchAccumMouse = 0f;
            _mouseSwitchArmed = true;
            return;
        }

        // 逆方向へ振り直したら蓄積を捨てて再武装
        if (_switchAccumMouse != 0f && Mathf.Sign(delta) != Mathf.Sign(_switchAccumMouse))
        {
            _switchAccumMouse = 0f;
            _mouseSwitchArmed = true;
        }

        _switchAccumMouse += delta;

        // 武装中に閾値を超えたら1回だけ切り替え
        if (_mouseSwitchArmed && Mathf.Abs(_switchAccumMouse) >= _switchMouseThreshold)
        {
            TrySwitchTarget(Mathf.Sign(_switchAccumMouse));
            _mouseSwitchArmed = false;
        }
    }

    /// <summary>指定方向のターゲットへ切り替える。対象がいなければ何もしない。</summary>
    private void TrySwitchTarget(float direction)
    {
        var next = _selector.SelectSwitchTarget(CurrentTarget, direction);
        if (next != null) LockOn(next);
    }

    /// <summary>対象切り替えの入力状態を初期化する。ロックオン開始時に呼ぶ。</summary>
    /// <remarks>ラッチは未武装で始め、入力がニュートラルに戻ってから初めて切り替えを受け付ける。</remarks>
    private void ResetSwitchState()
    {
        _stickSwitchArmed = false;
        _mouseSwitchArmed = false;
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
