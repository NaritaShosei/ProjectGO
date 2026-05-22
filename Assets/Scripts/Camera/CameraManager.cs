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
    /// <summary>メインカメラの参照</summary>
    public Camera MainCamera => _mainCamera;

    /// <summary>現在ロックオンしている対象</summary>
    public ILockOnTarget CurrentTarget => _currentTarget;

    /// <summary>現在ロックオン中かどうか</summary>
    public bool IsLockedOn => _currentTarget != null;

    /// <summary>ロックオン対象が変更された際の通知</summary>
    public event Action<ILockOnTarget> OnLockOnTargetChanged;

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

    // TODO: テスト用なのでちゃんと削除します
    public float LockOnAreaRadius => _lockOnAreaRadius;

    /// <summary>
    /// 指定したターゲットをロックオンします。
    /// </summary>
    public void LockOn(ILockOnTarget target)
    {
        if (target is not Component targetComponent || !targetComponent || !target.IsLockable || target.GetTargetCenter() == null)
        {
            Debug.LogWarning("ロックオン対象が無効です。");
            return;
        }

        if (_currentTarget == target) return;

        _currentTarget = target;
        _currentTargetComponent = targetComponent;
        _lockOnCamera.Priority = _lockOnPriority;

        // ブレンド開始状態を記録
        _blendStartPosition = _lockOnCamera.transform.position;
        _blendStartRotation = _mainCamera.transform.rotation;
        _blendT = 0f;
        _isLockOnBlending = true;

        OnLockOnTargetChanged?.Invoke(_currentTarget);
    }

    /// <summary>
    /// ロックオンを解除し、通常カメラに戻します。
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
    /// </summary>
    /// <param name="inputDirection">正で右、負で左</param>
    public void SwitchLockOnTarget(float inputDirection)
    {
        if (!IsLockedOn) return;

        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(_mainCamera);
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);

        // 現在のターゲットのスクリーン座標
        Vector3 currentScreenPos = _mainCamera.WorldToScreenPoint(
            _currentTarget.GetTargetCenter().position
        );

        ILockOnTarget best = null;
        float bestScore = float.MaxValue;

        foreach (var candidate in GetLockOnCandidates())
        {
            if (candidate == _currentTarget) continue;
            if (candidate is not Component comp) continue;
            if (!candidate.IsLockable || candidate.GetTargetCenter() == null) continue;

            // 距離チェック
            if (Vector3.Distance(_playerTransform.position, comp.transform.position) > _lockOnRange) continue;

            // 画面内チェック
            Bounds bounds = comp.GetComponent<Collider>()?.bounds ?? new Bounds(comp.transform.position, Vector3.one);
            if (!GeometryUtility.TestPlanesAABB(frustumPlanes, bounds)) continue;

            Vector3 screenPos = _mainCamera.WorldToScreenPoint(comp.transform.position);
            if (screenPos.z < 0) continue;

            // 入力方向チェック（右入力なら現在より右にいる敵のみ）
            float diff = screenPos.x - currentScreenPos.x;
            if (inputDirection > 0 && diff <= 0) continue;
            if (inputDirection < 0 && diff >= 0) continue;

            // 画面中央スコア
            float score = Vector2.Distance(new Vector2(screenPos.x, screenPos.y), screenCenter);

            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        if (best != null) LockOn(best);
    }

    public async UniTask ExecutionCameraShake(CameraShakeData data)
    {
        await _cameraShake.StartCameraShake(data);
    }

    public void ExecutionForceStopCameraShake()
    {
        _cameraShake.ForceStopCameraShake();
    }

    [Header("カメラ参照")]
    [SerializeField] private CinemachineCamera _normalCamera;
    [SerializeField] private CinemachineCamera _lockOnCamera;

    [Header("優先度設定")]
    [SerializeField] private int _normalPriority = 10;
    [SerializeField] private int _lockOnPriority = 20;

    [Header("追従設定")]
    [SerializeField] private float _cameraDistance = 5f;
    [SerializeField] private float _cameraHeight = 2f;
    [SerializeField] private float _posSmoothTime = 0.2f;
    [SerializeField] private float _cameraFollowSpeed = 15f; // カメラ位置の追従速度

    [Header("ロックオン設定")]
    [SerializeField] private float _lockOnRange = 20f;                  // ロック可能距離
    [SerializeField] private float _lockOnAreaRadius = 100f;            // 画面中央のエリア半径（px）
    [SerializeField] private float _lockOnFollowSpeedMin = 2f;          // エリア逸脱時の最小追従速度
    [SerializeField] private float _lockOnFollowSpeedMax = 10f;         // エリア逸脱時の最大追従速度
    [SerializeField] private float _lockOnFollowSpeedDistanceMax = 300f;// 最大速度になる逸脱距離（px）
    [SerializeField] private float _autoUnlockRange = 25f;              // 自動解除距離

    [Header("ロックオン開始ブレンド")]
    [SerializeField] private float _lockOnBlendDuration = 0.4f;
    [SerializeField, Range(1f, 8f)] private float _lockOnBlendExponent = 3f;

    private bool _isLockOnBlending = false;
    private float _blendT = 0f;
    private Vector3 _blendStartPosition;
    private Quaternion _blendStartRotation;

    private Camera _mainCamera;
    private Transform _playerTransform;
    private ILockOnTarget _currentTarget;
    private Component _currentTargetComponent;
    private CinemachineOrbitalFollow _normalOrbitalFollow;
    private Transform _cameraFollowTarget;
    private Vector3 _normalFollowVelocity;
    private Vector3 _lockOnCameraVelocity;
    private CameraShake _cameraShake;

    private float EaseOut(float t) => 1f - Mathf.Pow(1f - t, _lockOnBlendExponent);

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
        {
            UpdateLockOnCamera();
        }
        else
        {
            UpdateNormalCameraPosition();
        }
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<CameraManager>();
    }

    private void UpdateNormalCameraPosition()
    {
        _cameraFollowTarget.position = Vector3.SmoothDamp(
            _cameraFollowTarget.position,
            _playerTransform.position,
            ref _normalFollowVelocity,
            _posSmoothTime
        );
    }

    private void UpdateLockOnCamera()
    {
        // 対象の有効性チェック
        if (_currentTargetComponent == null || !_currentTarget.IsLockable || _currentTarget.GetTargetCenter() == null)
        {
            Unlock();
            return;
        }

        // 自動解除チェック
        float distToTarget = Vector3.Distance(_playerTransform.position, _currentTargetComponent.transform.position);
        if (distToTarget > _autoUnlockRange)
        {
            Unlock();
            return;
        }

        Transform targetCenter = _currentTarget.GetTargetCenter();

        // ブレンド中
        if (_isLockOnBlending)
        {
            _blendT += Time.fixedDeltaTime / _lockOnBlendDuration;
            float eased = EaseOut(Mathf.Clamp01(_blendT));

            // 目標位置・回転をその都度計算
            Vector3 desiredPos = CalcDesiredPosition(targetCenter);
            Quaternion desiredRot = CalcDesiredRotation(targetCenter, desiredPos);

            _lockOnCamera.transform.position = Vector3.Lerp(_blendStartPosition, desiredPos, eased);
            _lockOnCamera.transform.rotation = Quaternion.Slerp(_blendStartRotation, desiredRot, eased);

            if (_blendT >= 1f) _isLockOnBlending = false;
            return;
        }

        // ブレンド完了後：エリア判定ベースの追従
        UpdateLockOnCameraByArea(targetCenter);

        _cameraFollowTarget.position = _playerTransform.position;
    }

    /// <summary>
    /// ロックオンエリア判定に基づいてカメラを動かします。
    /// </summary>
    private void UpdateLockOnCameraByArea(Transform targetCenter)
    {
        // 位置は常に追従
        Vector3 desiredPos = CalcDesiredPosition(targetCenter);
        _lockOnCamera.transform.position = Vector3.MoveTowards(
            _lockOnCamera.transform.position,
            desiredPos,
            _cameraFollowSpeed * Time.fixedDeltaTime
        );

        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Vector3 screenPos = _mainCamera.WorldToScreenPoint(targetCenter.position);

        // カメラ後方に回った場合は強制追従
        if (screenPos.z < 0)
        {
            Quaternion desiredRot = CalcDesiredRotation(targetCenter, _lockOnCamera.transform.position);
            _lockOnCamera.transform.rotation = Quaternion.Slerp(
                _lockOnCamera.transform.rotation,
                desiredRot,
                Time.fixedDeltaTime * _lockOnFollowSpeedMax
            );
            return;
        }

        float deviation = Vector2.Distance(
            new Vector2(screenPos.x, screenPos.y),
            screenCenter
        );

        // エリア内なら何もしない
        if (deviation <= _lockOnAreaRadius) return;

        // 逸脱量に応じて速度を補間（エリア境界付近はMin、遠いほどMax）
        float t = Mathf.Clamp01(
            (deviation - _lockOnAreaRadius) / (_lockOnFollowSpeedDistanceMax - _lockOnAreaRadius)
        );
        float followSpeed = Mathf.Lerp(_lockOnFollowSpeedMin, _lockOnFollowSpeedMax, t);

        Quaternion targetRot = CalcDesiredRotation(targetCenter, _lockOnCamera.transform.position);
        _lockOnCamera.transform.rotation = Quaternion.Slerp(
            _lockOnCamera.transform.rotation,
            targetRot,
            Time.fixedDeltaTime * followSpeed
        );
    }

    /// <summary>
    /// ロックオン時のカメラ目標位置を計算します。
    /// </summary>
    private Vector3 CalcDesiredPosition(Transform targetCenter)
    {
        // プレイヤーが向いている方向の真後ろ
        Vector3 back = -_playerTransform.forward;
        back.y = 0;
        back.Normalize();

        return _playerTransform.position
             + (back * _cameraDistance)
             + (Vector3.up * _cameraHeight);
    }
    /// <summary>
    /// ロックオン時のカメラ目標回転を計算します。
    /// </summary>
    private Quaternion CalcDesiredRotation(Transform targetCenter, Vector3 fromPosition)
    {
        Vector3 lookAtPoint = Vector3.Lerp(_playerTransform.position, targetCenter.position, 0.5f);
        Vector3 dir = lookAtPoint - fromPosition;
        if (dir.sqrMagnitude < 0.001f) return _lockOnCamera.transform.rotation;
        return Quaternion.LookRotation(dir);
    }

    private void ApplyRotationToNormalCamera()
    {
        if (_normalOrbitalFollow == null) return;

        Vector3 currentEuler = _lockOnCamera.transform.rotation.eulerAngles;
        _normalOrbitalFollow.HorizontalAxis.Value = currentEuler.y;
        _normalOrbitalFollow.VerticalAxis.Value = currentEuler.x;
    }

    /// <summary>
    /// ロックオン候補を取得します。LockOnDetector等に差し替えてください。
    /// </summary>
    private ILockOnTarget[] GetLockOnCandidates()
    {
        // TODO: LockOnDetectorなど候補管理クラスから取得する実装に差し替える
        return FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None) as ILockOnTarget[];
    }
}