using UnityEngine;

/// <summary>
/// ロックオンの状態管理を担うクラス。
/// 手動ロックオン・自動ロックオン・ターゲット切り替え・解除を一元管理します。
/// ターゲット選択はLockOnTargetSelectorに、カメラ操作はCameraManagerに委譲します。
/// </summary>
public class LockOnController : MonoBehaviour
{
    #region Inspectorフィールド

    [Tooltip("攻撃後にロックオンが継続する時間（秒）。この時間内に次の攻撃が来なければ自動解除する")]
    [SerializeField] private float _autoLockOnDuration = 2f;

    [Tooltip("ロックオン可能な最大距離（m）")]
    [SerializeField] private float _lockOnRange = 20f;

    #endregion

    #region プライベートフィールド

    private CameraManager _cameraManager;
    private InputHandler _inputHandler;
    private LockOnTargetSelector _selector;

    private bool _isAutoLockOn;       // 自動ロックオン中かどうか
    private float _autoLockOnTimer;   // 自動ロックオン残り時間

    #endregion

    #region 初期化

    public void Init(CameraManager cameraManager, InputHandler inputHandler, EnemyManager enemyManager, Transform playerTransform)
    {
        _cameraManager = cameraManager;
        _inputHandler = inputHandler;

        _selector = new LockOnTargetSelector(
            cameraManager.MainCamera,
            playerTransform,
            _lockOnRange,
            enemyManager
        );

        SubscribeInputEvents();

        // 敵を倒したときの次ターゲット自動選択
        enemyManager.OnEnemyDefeated += HandleEnemyDefeated;
    }

    private void SubscribeInputEvents()
    {
        _inputHandler.OnLockOn += HandleLockOnInput;
        _inputHandler.OnLockOnLeft += HandleLockOnLeft;
        _inputHandler.OnLockOnRight += HandleLockOnRight;
        _inputHandler.OnLightAttack += HandleAttackInput;
        _inputHandler.OnChargeStart += HandleAttackInput;
    }

    #endregion

    #region Unityライフサイクル

    private void Update()
    {
        if (!_isAutoLockOn) return;

        _autoLockOnTimer -= Time.deltaTime;
        if (_autoLockOnTimer <= 0f)
        {
            EndAutoLockOn();
        }
    }

    private void OnDestroy()
    {
        if (_inputHandler == null) return;

        _inputHandler.OnLockOn -= HandleLockOnInput;
        _inputHandler.OnLockOnLeft -= HandleLockOnLeft;
        _inputHandler.OnLockOnRight -= HandleLockOnRight;
        _inputHandler.OnLightAttack -= HandleAttackInput;
        _inputHandler.OnChargeStart -= HandleAttackInput;
    }

    #endregion

    #region 入力ハンドラ

    /// <summary>
    /// ロックオンボタン入力。
    /// ロックオン中は解除、未ロックオン時は手動ロックオン開始。
    /// </summary>
    private void HandleLockOnInput()
    {
        if (_cameraManager.IsLockedOn)
        {
            // 自動ロックオン中に手動ロックオンボタンを押した場合も解除
            _isAutoLockOn = false;
            _cameraManager.Unlock();
        }
        else
        {
            TryManualLockOn();
        }
    }

    /// <summary>
    /// 攻撃入力。自動ロックオンを開始またはタイマーをリセットする。
    /// 手動ロックオン中は何もしない。
    /// </summary>
    private void HandleAttackInput()
    {
        // 手動ロックオン中は自動ロックオンに干渉しない
        if (_cameraManager.IsLockedOn && !_isAutoLockOn) return;

        TryAutoLockOn();
    }

    /// <summary>左方向へのターゲット切り替え。</summary>
    private void HandleLockOnLeft()
    {
        if (!_cameraManager.IsLockedOn) return;

        var next = _selector.SelectSwitchTarget(_cameraManager.CurrentTarget, inputDirection: -1f);
        if (next == null) return;

        // 切り替え時は自動ロックオン状態を引き継がない
        _isAutoLockOn = false;
        _cameraManager.LockOn(next);
    }

    /// <summary>右方向へのターゲット切り替え。</summary>
    private void HandleLockOnRight()
    {
        if (!_cameraManager.IsLockedOn) return;

        var next = _selector.SelectSwitchTarget(_cameraManager.CurrentTarget, inputDirection: 1f);
        if (next == null) return;

        _isAutoLockOn = false;
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

        _isAutoLockOn = false;
        _cameraManager.LockOn(target);
    }

    /// <summary>
    /// 自動ロックオンを試みます。
    /// すでに自動ロックオン中であればタイマーをリセットします。
    /// 対象が見つからなければ何もしません。
    /// </summary>
    private void TryAutoLockOn()
    {
        // すでに自動ロックオン中ならタイマーだけリセット
        if (_isAutoLockOn)
        {
            _autoLockOnTimer = _autoLockOnDuration;
            return;
        }

        var target = _selector.SelectNearestTarget();
        if (target == null) return;

        _isAutoLockOn = true;
        _autoLockOnTimer = _autoLockOnDuration;
        _cameraManager.LockOn(target);
    }

    /// <summary>
    /// 自動ロックオンをタイムアウトで終了します。
    /// </summary>
    private void EndAutoLockOn()
    {
        _isAutoLockOn = false;
        _cameraManager.Unlock();
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
            _isAutoLockOn = false;
            _cameraManager.Unlock();
        }
    }

    #endregion
}