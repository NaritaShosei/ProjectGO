using BossEnemy.Enum;
using BossEnemy.Interface;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ボス戦中だけ有効化する専用カメラの制御。
/// カメラの定位置はプレイヤーから見てボスの反対側へ自動追従させ、入力では注視点を左右へわずかに振るだけに留める。
/// 注視の基準はボス子階層の CameraAnglePoint、ズームは IBossEnemyCharacterView.OnChangedPosture（姿勢）を参照する。
/// </summary>
public sealed class BossCameraController
{
    /// <summary>ボスカメラが有効（ボス戦中）かどうか。</summary>
    public bool IsActive => _isActive;

    /// <summary>ボスカメラ制御を生成し、敵スポーン・ボス撃破イベントを購読する。</summary>
    public BossCameraController(
        CameraManager cameraManager,
        CinemachineCamera bossBodyCamera,
        Transform followAnchor,
        Camera mainCamera,
        InputHandler inputHandler,
        EnemyManager enemyManager,
        Transform playerTransform,
        BossCameraSettings settings)
    {
        _cameraManager = cameraManager;
        _bossBodyCamera = bossBodyCamera;
        _followAnchor = followAnchor;
        _mainCamera = mainCamera;
        _inputHandler = inputHandler;
        _enemyManager = enemyManager;
        _playerTransform = playerTransform;
        _settings = settings;

        // オービット・入力コンポーネントを取得
        _orbitalFollow = _bossBodyCamera.GetComponent<CinemachineOrbitalFollow>();
        _inputAxisController = _bossBodyCamera.GetComponent<CinemachineInputAxisController>();
        // Cinemachine既定の入力適用は使わず自前で回す
        if (_inputAxisController != null) _inputAxisController.enabled = false;
        if (_orbitalFollow == null) Debug.LogError("[BossCameraController] CinemachineOrbitalFollow が BossBodyCamera にありません。", _bossBodyCamera);

        // 注視点プロキシを生成しカメラと同じシーンへ移す
        _lookAtProxy = new GameObject("BossCameraLookAtProxy").transform;
        SceneManager.MoveGameObjectToScene(_lookAtProxy.gameObject, _bossBodyCamera.gameObject.scene);

        // 敵スポーン・ボス撃破・強制削除を購読
        _enemyManager.OnEnemySpawned += HandleEnemySpawned;
        _enemyManager.OnBossDefeated += HandleBossDefeated;
        _enemyManager.OnEnemyForceRemoved += HandleEnemyForceRemoved;
    }

    /// <summary>シーン切り替え後のメインカメラを更新する。</summary>
    public void SetMainCamera(Camera mainCamera)
    {
        if (mainCamera != null) _mainCamera = mainCamera;
    }

    /// <summary>有効時のみ、追従アンカー・定位置追従・注視点の左右スイベルを更新する。</summary>
    public void Tick(float deltaTime)
    {
        if (!_isActive) return;

        // ボスが破棄されていたら無効化（イベント取りこぼしの保険）
        var bossObject = _boss as Object;
        if (bossObject == null)
        {
            Deactivate();
            return;
        }

        // 通常カメラのTickは停止中なので追従アンカーを自前でプレイヤーへ寄せる
        _followAnchor.position = _playerTransform.position;

        UpdateOrbitTracking(deltaTime);
        UpdateSwivelOffset(deltaTime);
        UpdateLookAtProxy(deltaTime);
    }

    /// <summary>イベント購読を解除し、生成したプロキシを破棄する。</summary>
    public void Dispose()
    {
        if (_enemyManager != null)
        {
            _enemyManager.OnEnemySpawned -= HandleEnemySpawned;
            _enemyManager.OnBossDefeated -= HandleBossDefeated;
            _enemyManager.OnEnemyForceRemoved -= HandleEnemyForceRemoved;
        }

        UnsubscribePosture();

        if (_lookAtProxy != null) Object.Destroy(_lookAtProxy.gameObject);
    }

    /// <summary>スポーンした敵がボスなら参照・頭足アンカー・姿勢イベントを保持し、ボスカメラを有効化する。</summary>
    private void HandleEnemySpawned(IEnemy enemy)
    {
        if (enemy == null || !enemy.IsBoss) return;

        _boss = enemy;
        _bossView = enemy as IBossEnemyCharacterView;
        CollectAnglePoints(enemy.Self);

        // 姿勢変化を購読してズームへ反映する
        if (_bossView != null) _bossView.OnChangedPosture += HandleChangedPosture;

        Activate();
    }

    /// <summary>ボス撃破でボスカメラを無効化する。</summary>
    private void HandleBossDefeated() => Deactivate();

    /// <summary>現在のボスが強制削除されたらボスカメラを無効化する。</summary>
    private void HandleEnemyForceRemoved(IEnemy enemy)
    {
        if (ReferenceEquals(enemy, _boss)) Deactivate();
    }

    /// <summary>ボスの子階層から頭(Top)・足(Under)のカメラアングル基準点を取得する。</summary>
    private void CollectAnglePoints(Transform bossRoot)
    {
        _angleTop = null;
        _angleUnder = null;

        // 子階層のCameraAnglePointを種類で仕分け
        foreach (CameraAnglePoint point in bossRoot.GetComponentsInChildren<CameraAnglePoint>(true))
        {
            if (point.AnglePoint == CameraAnglePointType.Top) _angleTop = point.GetTargetCenter();
            else if (point.AnglePoint == CameraAnglePointType.Under) _angleUnder = point.GetTargetCenter();
        }

        if (_angleTop == null || _angleUnder == null)
            Debug.LogWarning("[BossCameraController] CameraAnglePoint(Top/Under) がボスに見つかりません。注視は体中心へフォールバックします。", _bossBodyCamera);
    }

    /// <summary>ボスカメラを最前面へ出し、追従・注視・水平軸初期値を設定する。</summary>
    private void Activate()
    {
        if (_isActive) return;

        // 追従はプレイヤー中心アンカー、注視はプロキシ
        _bossBodyCamera.Follow = _followAnchor;
        _bossBodyCamera.LookAt = _lookAtProxy;

        // 足元⇔頭の補間量を現在の距離へスナップしておく（有効化直後に寄っていかないように）
        _framingT = _angleTop != null && _angleUnder != null ? ComputeFramingTarget() : 0f;
        _framingTVelocity = 0f;

        // 注視点を現在の距離で初期化
        UpdateLookAtProxy(0f);
        // 現在のメインカメラ方位へ水平軸を合わせて切り替えの飛びを抑える
        AlignHorizontalAxisToCurrentView();

        _isActive = true;
        _swivelOffset = 0f;
        _cameraManager.SetBossCameraActive(true);

        // 現在値のgetterが無いので初期姿勢はStanding想定でズームを当て、以降はイベントで補正する
        HandleChangedPosture(PostureType.Standing);
    }

    /// <summary>ボスカメラを背面へ下げ、通常視野へ戻し、ボス参照を手放す。</summary>
    private void Deactivate()
    {
        if (!_isActive) return;

        _isActive = false;
        _cameraManager.SetBossCameraActive(false);
        _cameraManager.ResetZoom(_settings.ZoomResetDuration);

        UnsubscribePosture();
        _boss = null;
        _angleTop = null;
        _angleUnder = null;
    }

    /// <summary>姿勢に対応するFOV倍率を探してズームへ反映する。一致が無ければ何もしない。</summary>
    private void HandleChangedPosture(PostureType posture)
    {
        if (_settings.PostureZooms == null) return;

        foreach (BossPostureZoom entry in _settings.PostureZooms)
        {
            if (entry == null || entry.Posture != posture) continue;
            _cameraManager.SetZoom(entry.ZoomMultiplier, entry.Duration);
            return;
        }
    }

    /// <summary>ボスの姿勢イベント購読を解除する。</summary>
    private void UnsubscribePosture()
    {
        if (_bossView != null) _bossView.OnChangedPosture -= HandleChangedPosture;
        _bossView = null;
    }

    /// <summary>足元(Under)〜頭(Top)の補間量を距離目標へ緩やかに寄せ、注視点を更新して左右スイベル分だけ横にずらす。</summary>
    private void UpdateLookAtProxy(float deltaTime)
    {
        // アンカーが無ければ体中心へフォールバック（スイベルは適用しない）
        if (_angleTop == null || _angleUnder == null)
        {
            Transform center = _boss.GetTargetCenter();
            _lookAtProxy.position = center != null ? center.position : _boss.Self.position;
            return;
        }

        // 距離目標へ補間量を緩やかに追従させる（一気に足元⇔頭へ飛ばない）
        _framingT = Mathf.SmoothDamp(
            _framingT, ComputeFramingTarget(), ref _framingTVelocity, _settings.FramingSmoothTime, Mathf.Infinity, deltaTime);

        Vector3 basePosition = Vector3.Lerp(_angleUnder.position, _angleTop.position, _framingT);

        // 注視点をカメラ右方向へスイベル分だけずらす（オービット自体は動かさない）
        Vector3 cameraRight = _mainCamera != null ? _mainCamera.transform.right : Vector3.right;
        _lookAtProxy.position = basePosition + cameraRight * _swivelOffset;
    }

    /// <summary>プレイヤー↔ボス距離を0(足元)〜1(頭)へ正規化した注視補間量の目標値。</summary>
    private float ComputeFramingTarget()
    {
        float distance = Vector3.Distance(_playerTransform.position, _boss.Self.position);
        return Mathf.InverseLerp(_settings.FramingNearDistance, _settings.FramingFarDistance, distance);
    }

    /// <summary>カメラの定位置を、プレイヤーから見てボスの反対側の方位へ滑らかに追従させる。</summary>
    private void UpdateOrbitTracking(float deltaTime)
    {
        if (_orbitalFollow == null) return;

        Vector3 toBoss = _boss.Self.position - _playerTransform.position;
        toBoss.y = 0f;
        if (toBoss.sqrMagnitude <= 0.0001f) return;

        // カメラがボスへ正対する方位角へ、上限速度で寄せる
        float baseYaw = Mathf.Atan2(toBoss.x, toBoss.z) * Mathf.Rad2Deg;
        _orbitalFollow.HorizontalAxis.Value = Mathf.MoveTowardsAngle(
            _orbitalFollow.HorizontalAxis.Value, baseYaw, _settings.OrbitTrackSpeed * deltaTime);
    }

    /// <summary>入力で注視点の左右オフセットを一定範囲だけ動かす。入力が無ければ中央へ戻す。</summary>
    private void UpdateSwivelOffset(float deltaTime)
    {
        float inputX = _inputHandler.CameraMoveInput.x;

        if (Mathf.Abs(inputX) <= 0.0001f)
        {
            _swivelOffset = Mathf.MoveTowards(_swivelOffset, 0f, _settings.SwivelReturnSpeed * deltaTime);
            return;
        }

        _swivelOffset = Mathf.Clamp(
            _swivelOffset + inputX * _settings.SwivelSpeed * deltaTime,
            -_settings.SwivelRange,
            _settings.SwivelRange);
    }

    /// <summary>ボスカメラの水平軸を現在のメインカメラ方位に合わせる。</summary>
    private void AlignHorizontalAxisToCurrentView()
    {
        if (_orbitalFollow == null || _mainCamera == null) return;
        _orbitalFollow.HorizontalAxis.Value = _mainCamera.transform.eulerAngles.y;
    }

    private readonly CameraManager _cameraManager;
    private readonly CinemachineCamera _bossBodyCamera;
    private readonly Transform _followAnchor;
    private readonly InputHandler _inputHandler;
    private readonly EnemyManager _enemyManager;
    private readonly Transform _playerTransform;
    private readonly BossCameraSettings _settings;
    private readonly CinemachineOrbitalFollow _orbitalFollow;
    private readonly CinemachineInputAxisController _inputAxisController;
    private readonly Transform _lookAtProxy;

    private Camera _mainCamera;
    private IEnemy _boss;
    private IBossEnemyCharacterView _bossView;
    private Transform _angleTop;
    private Transform _angleUnder;
    private bool _isActive;
    private float _swivelOffset;
    private float _framingT;
    private float _framingTVelocity;
}
