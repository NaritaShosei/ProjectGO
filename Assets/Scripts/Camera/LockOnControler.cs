using UnityEngine;

/// <summary>
/// ロックオンの状態管理を担うクラス。
/// 手動ロックオン・自動ロックオン・ターゲット切り替え・解除を一元管理します。
/// ターゲット選択はLockOnTargetSelectorに、カメラ操作はCameraManagerに委譲します。
/// </summary>
public class LockOnController : MonoBehaviour
{
    #region Inspectorフィールド

    [Tooltip("ロックオン可能な最大距離（m）")]
    [SerializeField] private float _lockOnRange = 20f;

    #endregion

    #region プライベートフィールド

    private CameraManager _cameraManager;
    private InputHandler _inputHandler;
    private LockOnTargetSelector _selector;

    #endregion

    #region 初期化

    public void Init(CameraManager cameraManager, InputHandler inputHandler, EnemyManager enemyManager, Transform playerTransform)
    {
        _cameraManager = cameraManager;
        _inputHandler = inputHandler;

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

    private void SubscribeInputEvents()
    {
        _inputHandler.OnLockOn += HandleLockOnInput;
        _inputHandler.OnLockOnLeft += HandleLockOnLeft;
        _inputHandler.OnLockOnRight += HandleLockOnRight;
    }

    #endregion

    #region Unityライフサイクル


    private void OnDestroy()
    {
        if (_inputHandler == null) return;

        _inputHandler.OnLockOn -= HandleLockOnInput;
        _inputHandler.OnLockOnLeft -= HandleLockOnLeft;
        _inputHandler.OnLockOnRight -= HandleLockOnRight;

        var enemyManager = ServiceLocator.Get<EnemyManager>();
        if (enemyManager != null)
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
        Debug.Log($"[LockOnController] HandleLockOnInput | IsLockedOn: {_cameraManager.IsLockedOn}");
        if (_cameraManager.IsLockedOn)
        {
            // 自動ロックオン中に手動ロックオンボタンを押した場合も解除
            _cameraManager.Unlock();
        }
        else
        {
            TryManualLockOn();
        }
    }
    /// <summary>左方向へのターゲット切り替え。</summary>
    private void HandleLockOnLeft()
    {
        if (!_cameraManager.IsLockedOn) return;

        var next = _selector.SelectSwitchTarget(_cameraManager.CurrentTarget, inputDirection: -1f);
        if (next == null) return;

        _cameraManager.LockOn(next);
    }

    /// <summary>右方向へのターゲット切り替え。</summary>
    private void HandleLockOnRight()
    {
        if (!_cameraManager.IsLockedOn) return;

        var next = _selector.SelectSwitchTarget(_cameraManager.CurrentTarget, inputDirection: 1f);
        if (next == null) return;

        _cameraManager.LockOn(next);
    }

    #endregion

    #region ロックオン操作

    /// <summary>
    /// 手動ロックオンを試みます。
    /// 対象が見つからなければ何もしません。
    /// </summary>
    private void TryManualLockOn()
    {
        var target = _selector.SelectInitialTarget();
        if (target == null) return;

        _cameraManager.LockOn(target);
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
        if (!_cameraManager.IsLockedOn) return;

        var next = _selector.SelectNextTarget(_cameraManager.CurrentTarget);
        if (next != null)
        {
            // 自動ロックオン状態は引き継ぐ
            _cameraManager.LockOn(next);
        }
        else
        {
            _cameraManager.Unlock();
        }
    }

    private void HandleEnemyForceRemoved(IEnemy removedEnemy)
    {
        if (!_cameraManager.IsLockedOn) return;
        if (_cameraManager.CurrentTarget != removedEnemy) return;

        var next = _selector.SelectNextTarget(removedEnemy);
        if (next != null)
        {
            _cameraManager.LockOn(next);
        }
        else
        {
            _cameraManager.Unlock();
        }
    }

    #endregion
}
