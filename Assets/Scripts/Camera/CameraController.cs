using UnityEngine;

/// <summary>
/// カメラ入力とロックオン状態の遷移を管理するControllerです。
/// ターゲット選択はLockOnTargetSelectorに、カメラの動きは各Stateに委譲します。
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

        // 敵を倒したときの次ターゲット自動選択
        enemyManager.OnEnemyDefeated += HandleEnemyDefeated;
        enemyManager.OnEnemyForceRemoved += HandleEnemyForceRemoved;
    }

    /// <summary>現在のカメラ状態を更新します。</summary>
    public void Tick(float timeScale)
    {
        if (_currentState == null) return;

        if (_currentState == _lockOnState
            && (!_lockOnState.IsTargetValid || _lockOnState.IsTargetOutOfRange))
        {
            Unlock();
            return;
        }

        _currentState.Tick(timeScale, _inputHandler.CameraMoveInput);
    }

    /// <summary>指定した対象へロックオンします。</summary>
    public void LockOn(ILockOnTarget target)
    {
        if (target == null || !target.IsLockable || target.GetTargetCenter() == null)
            return;

        if (CurrentTarget == target) return;

        if (_currentState != _normalState)
        {
            _currentState.Exit();
        }

        _lockOnState.SetTarget(target);
        _currentState = _lockOnState;
        _cameraManager.SetLockOnCameraActive(true);
        _currentState.Enter();
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

    private CameraManager _cameraManager;
    private InputHandler _inputHandler;
    private LockOnTargetSelector _selector;
    private CameraMotionController _motionController;
    private NormalCameraState _normalState;
    private LockOnCameraState _lockOnState;
    private ICameraState _currentState;

    #endregion

    #region プライベートメソッド

    private void SubscribeInputEvents()
    {
        _inputHandler.OnLockOn += HandleLockOnInput;
        _inputHandler.OnLockOnLeft += HandleLockOnLeft;
        _inputHandler.OnLockOnRight += HandleLockOnRight;
    }

    #region Unityライフサイクル


    private void OnDestroy()
    {
        if (_inputHandler != null)
        {
            _inputHandler.OnLockOn -= HandleLockOnInput;
            _inputHandler.OnLockOnLeft -= HandleLockOnLeft;
            _inputHandler.OnLockOnRight -= HandleLockOnRight;
        }

        if (ServiceLocator.TryGet(out EnemyManager enemyManager))
        {
            enemyManager.OnEnemyDefeated -= HandleEnemyDefeated;
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
    /// <summary>左方向へのターゲット切り替え。</summary>

    private void HandleLockOnLeft()
    {
        if (_cameraManager == null || _selector == null) return;
        if (!_cameraManager.IsLockedOn) return;

        var next = _selector.SelectSwitchTarget(_cameraManager.CurrentTarget, inputDirection: -1f);
        if (next == null) return;

        LockOn(next);
    }

    /// <summary>右方向へのターゲット切り替え。</summary>
    private void HandleLockOnRight()
    {
        if (_cameraManager == null || _selector == null) return;
        if (!_cameraManager.IsLockedOn) return;

        var next = _selector.SelectSwitchTarget(_cameraManager.CurrentTarget, inputDirection: 1f);
        if (next == null) return;

        LockOn(next);
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

    /// <summary>
    /// 敵撃破時の処理。
    /// ロックオン中であれば次のターゲットを自動選択します。
    /// 次のターゲットが見つからなければロックオンを解除します。
    /// </summary>
    private void HandleEnemyDefeated()
    {
        if (!IsLockedOn) return;

        var next = _selector.SelectNextTarget(CurrentTarget);
        if (next != null)
        {
            // 自動ロックオン状態は引き継ぐ
            LockOn(next);
        }
        else
        {
            Unlock();
        }
    }

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
