using System;
using Cysharp.Threading.Tasks;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// カメラの挙動を管理するクラス。
/// 通常時の追従遅延およびロックオン時のターゲット追従を制御します。
/// </summary>
public class CameraManager : MonoBehaviour
{
    #region パブリックプロパティ・イベント

    /// <summary>メインカメラの参照</summary>
    public Camera MainCamera => _mainCamera;

    /// <summary>現在ロックオンしている対象</summary>
    public ILockOnTarget CurrentTarget => _currentTarget;

    /// <summary>現在ロックオン中かどうか</summary>
    public bool IsLockedOn => _currentTarget != null;

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

        _playerTransform = player.transform;
        _cameraFollowTarget.position = _playerTransform.position;
        _normalCamera.Follow = _cameraFollowTarget;
    }

    /// <summary>
    /// 指定したターゲットをロックオンします。
    /// ロックオン開始時はEaseOutブレンドで滑らかにカメラを切り替えます。
    /// </summary>
    public void LockOn(ILockOnTarget target)
    {
        if (!IsValidTarget(target)) return;
        if (_currentTarget == target) return;

        var targetComponent = target as Component;
        _currentTarget = target;
        _currentTargetComponent = targetComponent;
        _lockOnCamera.Priority = _lockOnPriority;

        BeginLockOnBlend();

        OnLockOnTargetChanged?.Invoke(_currentTarget);
    }

    /// <summary>
    /// ロックオンを解除し、通常カメラに戻します。
    /// 解除時は現在のカメラ角度を通常カメラに引き継ぎます。
    /// </summary>
    public void Unlock()
    {
        if (_currentTarget == null && _currentTargetComponent == null) return;

        ApplyRotationToNormalCamera();

        _currentTarget = null;
        _currentTargetComponent = null;
        _lockOnCamera.Priority = _normalPriority - 1;
        _isLockOnBlending = false;

        OnLockOnTargetChanged?.Invoke(null);
    }

    /// <summary>
    /// 入力方向に応じてロックオン対象を切り替えます。
    /// 画面内かつ入力方向側にいる敵の中で、画面中央に最も近いものを選びます。
    /// </summary>
    /// <param name="inputDirection">正で右、負で左</param>
    public void SwitchLockOnTarget(float inputDirection)
    {
        if (!IsLockedOn) return;

        ILockOnTarget best = FindSwitchTarget(inputDirection);
        if (best != null) LockOn(best);
    }

    /// <summary>カメラシェイクを実行します。</summary>
    public async UniTask ExecutionCameraShake(CameraShakeData data)
    {
        await _cameraShake.StartCameraShake(data);
    }

    /// <summary>カメラシェイクを強制停止します。</summary>
    public void ExecutionForceStopCameraShake()
    {
        _cameraShake.ForceStopCameraShake();
    }

    #endregion

    #region Inspectorフィールド

    [Header("カメラ参照")]
    [SerializeField] private CinemachineCamera _normalCamera;
    [SerializeField] private CinemachineCamera _lockOnCamera;

    [Header("優先度設定")]
    [SerializeField] private int _normalPriority = 10;
    [SerializeField] private int _lockOnPriority = 20;

    [Header("通常追従設定")]
    [SerializeField] private float _cameraDistance = 5f;
    [SerializeField] private float _cameraHeight = 2f;
    [SerializeField] private float _posSmoothTime = 0.2f;         // 通常時の位置遅延時間
    [SerializeField] private float _cameraFollowSpeed = 15f;      // ロックオン時の位置追従速度

    [Header("プレイヤー追従遊び設定")]
    [SerializeField] private float _playerAreaRadius = 80f;       // プレイヤーが画面上でこの範囲内にいる間はカメラをほぼ動かさない（px）
    [SerializeField] private float _playerFollowSpeedInner = 1f;  // 遊び範囲内での中心への戻り速度
    [SerializeField] private float _playerFollowSpeedOuter = 10f; // 遊び範囲外での追従速度

    [Header("ロックオン設定")]
    [SerializeField] private float _lockOnRange = 20f;                   // ロック可能な最大距離
    [SerializeField] private float _lockOnAreaRadius = 100f;             // ターゲットが収まるべき画面中央エリアの半径（px）
    [SerializeField] private float _lockOnFollowSpeedMin = 2f;           // エリア逸脱時の最小回転追従速度
    [SerializeField] private float _lockOnFollowSpeedMax = 10f;          // エリア逸脱時の最大回転追従速度
    [SerializeField] private float _lockOnFollowSpeedDistanceMax = 300f; // この逸脱距離（px）で最大速度に達する
    [SerializeField] private float _autoUnlockRange = 25f;               // この距離を超えると自動でロックオン解除

    [Header("ロックオン開始ブレンド")]
    [SerializeField] private float _lockOnBlendDuration = 0.4f;
    [SerializeField, Range(1f, 8f)] private float _lockOnBlendExponent = 3f;

    #endregion

    #region プライベートフィールド

    private Camera _mainCamera;
    private Transform _playerTransform;
    private ILockOnTarget _currentTarget;
    private Component _currentTargetComponent;
    private CinemachineOrbitalFollow _normalOrbitalFollow;
    private Transform _cameraFollowTarget;
    private Vector3 _normalFollowVelocity;
    private CameraShake _cameraShake;

    private bool _isLockOnBlending;
    private float _blendT;
    private Vector3 _blendStartPosition;
    private Quaternion _blendStartRotation;

    #endregion

    #region イージング関数

    /// <summary>最初速く、終わりにゆっくり収束する。ロックオン開始ブレンドに使用。</summary>
    private float EaseOut(float t) => 1f - Mathf.Pow(1f - t, _lockOnBlendExponent);

    /// <summary>最初ゆっくり、終わりに速くなる。プレイヤー追従の遊び範囲内で使用。</summary>
    private float EaseInCubic(float t) => t * t * t;

    #endregion

    #region Unityライフサイクル

    private void Awake()
    {
        _mainCamera = Camera.main;
        ServiceLocator.Register(this);

        _cameraShake = new CameraShake(_normalCamera);
        _cameraFollowTarget = new GameObject("CameraFollowTarget").transform;

        _normalCamera.Priority = _normalPriority;
        _lockOnCamera.Priority = _normalPriority - 1;

        _normalOrbitalFollow = _normalCamera.GetComponent<CinemachineOrbitalFollow>();
    }

    private void FixedUpdate()
    {
        if (_playerTransform == null) return;

        if (IsLockedOn)
            UpdateLockOnCamera();
        else
            UpdateNormalCameraPosition();
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<CameraManager>();
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
    /// エリア判定に基づいてカメラの位置と回転を更新します。
    /// 位置はプレイヤーの画面上の逸脱量、回転はターゲットの逸脱量でそれぞれ制御します。
    /// </summary>
    private void UpdateLockOnCameraByArea(Transform targetCenter)
    {
        UpdateCameraPosition();
        UpdateCameraRotation(targetCenter);
    }

    /// <summary>
    /// プレイヤーの画面上の位置に応じてカメラ位置を更新します。
    /// 遊び範囲内ではEaseInで緩やかに中心へ戻り、範囲外では速く追従します。
    /// </summary>
    private void UpdateCameraPosition()
    {
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Vector2 playerScreenPos = GetScreenPos(_playerTransform.position);
        float deviation = Vector2.Distance(playerScreenPos, screenCenter);

        float speed = deviation > _playerAreaRadius
            ? _playerFollowSpeedOuter
            : _playerFollowSpeedInner * EaseInCubic(deviation / _playerAreaRadius);

        _lockOnCamera.transform.position = Vector3.MoveTowards(
            _lockOnCamera.transform.position,
            CalcDesiredPosition(),
            speed * Time.fixedDeltaTime
        );
    }

    /// <summary>
    /// ターゲットの画面上の位置に応じてカメラ回転を更新します。
    /// ロックオンエリア内では回転せず、エリア外に出た逸脱量に応じて速度が上がります。
    /// ターゲットがカメラ後方に回り込んだ場合は最大速度で強制追従します。
    /// </summary>
    private void UpdateCameraRotation(Transform targetCenter)
    {
        Vector3 rawScreenPos = _mainCamera.WorldToScreenPoint(targetCenter.position);

        // カメラ後方に回った場合は最大速度で強制追従
        if (rawScreenPos.z < 0)
        {
            RotateToward(targetCenter, _lockOnFollowSpeedMax);
            return;
        }

        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        float deviation = Vector2.Distance(new Vector2(rawScreenPos.x, rawScreenPos.y), screenCenter);

        if (deviation <= _lockOnAreaRadius) return;

        float t = Mathf.Clamp01((deviation - _lockOnAreaRadius) / (_lockOnFollowSpeedDistanceMax - _lockOnAreaRadius));
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

    #region ターゲット選定

    /// <summary>
    /// 入力方向側にいる画面内の敵の中から、画面中央に最も近いものを返します。
    /// </summary>
    private ILockOnTarget FindSwitchTarget(float inputDirection)
    {
        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(_mainCamera);
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Vector3 currentScreenPos = _mainCamera.WorldToScreenPoint(_currentTarget.GetTargetCenter().position);

        ILockOnTarget best = null;
        float bestScore = float.MaxValue;

        foreach (var candidate in GetLockOnCandidates())
        {
            if (candidate == _currentTarget) continue;
            if (candidate is not Component comp) continue;
            if (!candidate.IsLockable || candidate.GetTargetCenter() == null) continue;
            if (Vector3.Distance(_playerTransform.position, comp.transform.position) > _lockOnRange) continue;

            Bounds bounds = comp.GetComponent<Collider>()?.bounds ?? new Bounds(comp.transform.position, Vector3.one);
            if (!GeometryUtility.TestPlanesAABB(frustumPlanes, bounds)) continue;

            Vector3 screenPos = _mainCamera.WorldToScreenPoint(comp.transform.position);
            if (screenPos.z < 0) continue;

            // 入力方向と反対側にいる候補は除外
            float diff = screenPos.x - currentScreenPos.x;
            if (inputDirection > 0 && diff <= 0) continue;
            if (inputDirection < 0 && diff >= 0) continue;

            float score = Vector2.Distance(new Vector2(screenPos.x, screenPos.y), screenCenter);
            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best;
    }

    /// <summary>
    /// ロックオン候補を取得します。LockOnDetector等に差し替えてください。
    /// </summary>
    private ILockOnTarget[] GetLockOnCandidates()
    {
        // TODO: LockOnDetectorなど候補管理クラスから取得する実装に差し替える
        return FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None) as ILockOnTarget[];
    }

    #endregion

    #region ヘルパー

    /// <summary>
    /// ロックオン開始時のブレンド初期値を記録します。
    /// </summary>
    private void BeginLockOnBlend()
    {
        _blendStartPosition = _lockOnCamera.transform.position;
        _blendStartRotation = _mainCamera.transform.rotation;
        _blendT = 0f;
        _isLockOnBlending = true;
    }

    /// <summary>
    /// プレイヤーの真後ろにカメラを置くための目標位置を計算します。
    /// </summary>
    private Vector3 CalcDesiredPosition()
    {
        Vector3 back = -_playerTransform.forward;
        back.y = 0f;
        back.Normalize();

        return _playerTransform.position
             + (back * _cameraDistance)
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

    /// <summary>ワールド座標をスクリーン座標（Vector2）に変換します。</summary>
    private Vector2 GetScreenPos(Vector3 worldPos)
    {
        Vector3 raw = _mainCamera.WorldToScreenPoint(worldPos);
        return new Vector2(raw.x, raw.y);
    }

    /// <summary>ターゲットが有効なロックオン対象かチェックします。</summary>
    private bool IsValidTarget(ILockOnTarget target)
    {
        if (target is not Component comp || !comp || !target.IsLockable || target.GetTargetCenter() == null)
        {
            Debug.LogWarning("ロックオン対象が無効です。");
            return false;
        }
        return true;
    }

    /// <summary>現在のターゲットが有効な状態かチェックします。</summary>
    private bool IsCurrentTargetValid()
    {
        return _currentTargetComponent != null
            && _currentTarget.IsLockable
            && _currentTarget.GetTargetCenter() != null;
    }

    /// <summary>現在のターゲットが自動解除距離を超えているかチェックします。</summary>
    private bool IsTargetOutOfRange()
    {
        return Vector3.Distance(_playerTransform.position, _currentTargetComponent.transform.position) > _autoUnlockRange;
    }

    #endregion
}