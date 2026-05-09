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

    /// <summary>
    /// カメラマネージャーの初期化。
    /// </summary>
    /// <param name="player">追従対象となるプレイヤー</param>
    public void Init(Player player)
    {
        if (player == null)
        {
            Debug.LogError("Playerの参照がnullです。");
            return;
        }

        _playerTransform = player.transform;

        // 通常時の追従ターゲット初期位置を設定
        _cameraFollowTarget.position = _playerTransform.position;
        _normalCamera.Follow = _cameraFollowTarget;
    }

    /// <summary>
    /// 指定したターゲットをロックオンします。
    /// </summary>
    /// <param name="target">ロックオン対象</param>
    public void LockOn(ILockOnTarget target)
    {
        // ターゲットの有効性チェック
        if (target is not Component targetComponent || !targetComponent || !target.IsLockable || target.GetTargetCenter() == null)
        {
            Debug.LogWarning("ロックオン対象が無効です。");
            return;
        }

        if (_currentTarget == target) return;

        _currentTarget = target;
        _currentTargetComponent = targetComponent;
        _lockOnCamera.Priority = _lockOnPriority;

        OnLockOnTargetChanged?.Invoke(_currentTarget);
    }

    /// <summary>
    /// ロックオンを解除し、通常カメラに戻します。
    /// </summary>
    public void Unlock()
    {
        if (_currentTarget == null && _currentTargetComponent == null) return;

        // ロックオン解除時の角度を通常カメラに引き継ぐ
        ApplyRotationToNormalCamera();

        _currentTarget = null;
        _currentTargetComponent = null;
        _lockOnCamera.Priority = _normalPriority - 1;

        OnLockOnTargetChanged?.Invoke(null);
    }

    /// <summary>
    /// カメラシェイクの実行
    /// </summary>
    public async UniTask ExecutionCameraShake(float amplitude, float frequency, float duration)
    {
        CameraShakeData data = new CameraShakeData();

        data.amplitude += amplitude;
        data.frequency += frequency;
        data.duration += duration;

        await _cameraShake.StartCameraShake(data);
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
    [SerializeField] private float _posSmoothTime = 0.2f; // 位置の遅延時間
    [SerializeField] private float _rotFollowSpeed = 10f; // 回転の追従速度

    [Header("カメラシェイク設定")]
    [SerializeField] private float _amplitude = 1;//カメラ振幅
    [SerializeField] private float _frequency = 1;//カメラ振動周期
    [SerializeField] private float _duration = 1;//持続時間

    private Camera _mainCamera;
    private Transform _playerTransform;
    private ILockOnTarget _currentTarget;
    private Component _currentTargetComponent;
    private CinemachineOrbitalFollow _normalOrbitalFollow; // 通常カメラのOrbitalFollowコンポーネントへの参照
    private Transform _cameraFollowTarget; // 通常時の遅延追従用アンカー
    private Vector3 _normalFollowVelocity; // 通常追従用
    private Vector3 _lockOnCameraVelocity; // ロックオン追従用
    private CameraShake _cameraShake; //カメラシェイク

    private void Awake()
    {
        _mainCamera = Camera.main;
        ServiceLocator.Register(this);

        _cameraShake = new CameraShake(_normalCamera);

        // 追従遅延を実現するための仮想アンカーを生成
        _cameraFollowTarget = new GameObject("CameraFollowTarget").transform;

        _normalCamera.Priority = _normalPriority;
        _lockOnCamera.Priority = _normalPriority - 1;

        // 通常カメラのOrbitalFollowコンポーネントを取得
        _normalOrbitalFollow = _normalCamera.GetComponent<CinemachineOrbitalFollow>();
    }

    /// <summary>
    /// 物理演算の更新タイミングでカメラの座標計算を行います。
    /// </summary>
    private void FixedUpdate()
    {
        if (_playerTransform == null) return;

        if (IsLockedOn)
        {
            UpdateLockOnCameraPosition();
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

    /// <summary>
    /// 通常時の追従ターゲットを遅延させて移動させます。
    /// </summary>
    private void UpdateNormalCameraPosition()
    {
        // FixedUpdate内なので、本来は第4引数は不要（自動でfixedDeltaTime参照）ですが、
        // 明示的に扱う場合は注意が必要です。
        _cameraFollowTarget.position = Vector3.SmoothDamp(
            _cameraFollowTarget.position,
            _playerTransform.position,
            ref _normalFollowVelocity,
            _posSmoothTime
        );
    }

    /// <summary>
    /// ロックオン時の特殊なカメラ座標・回転を更新します。
    /// </summary>
    private void UpdateLockOnCameraPosition()
    {
        if (_currentTargetComponent == null || !_currentTarget.IsLockable || _currentTarget.GetTargetCenter() == null)
        {
            Unlock();
            return;
        }

        Transform targetCenter = _currentTarget.GetTargetCenter();

        Vector3 dirToPlayer = (_playerTransform.position - targetCenter.position);
        dirToPlayer.y = 0;

        if (dirToPlayer.sqrMagnitude < 0.001f) return;

        Vector3 desiredPosition = _playerTransform.position
                                  + (dirToPlayer.normalized * _cameraDistance)
                                  + (Vector3.up * _cameraHeight);

        // Positionの更新
        _lockOnCamera.transform.position = Vector3.SmoothDamp(
            _lockOnCamera.transform.position,
            desiredPosition,
            ref _lockOnCameraVelocity,
            _posSmoothTime
        );

        // Rotationの更新
        Vector3 lookAtPoint = Vector3.Lerp(_playerTransform.position, targetCenter.position, 0.5f);
        Quaternion targetRotation = Quaternion.LookRotation(lookAtPoint - _lockOnCamera.transform.position);

        // 【修正箇所】Time.deltaTime から Time.fixedDeltaTime に変更
        _lockOnCamera.transform.rotation = Quaternion.Slerp(
            _lockOnCamera.transform.rotation,
            targetRotation,
            Time.fixedDeltaTime * _rotFollowSpeed
        );

        _cameraFollowTarget.position = _playerTransform.position;
    }

    /// <summary>
    /// ロックオン解除時、現在のカメラ角度をOrbitalFollowコンポーネントに適用します。
    /// </summary>
    private void ApplyRotationToNormalCamera()
    {
        if (_normalOrbitalFollow == null) return;

        Vector3 currentEuler = _lockOnCamera.transform.rotation.eulerAngles;
        _normalOrbitalFollow.HorizontalAxis.Value = currentEuler.y;
        _normalOrbitalFollow.VerticalAxis.Value = currentEuler.x;
    }
}
