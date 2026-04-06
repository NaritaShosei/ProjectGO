using UnityEngine;

public class LockOnManager : MonoBehaviour
{
    [Header("ロックオン設定")]
    [SerializeField] private float _lockOnRange = 20f;
    [SerializeField] private LayerMask _lockOnLayer;

    [Header("ロックオン切り替え設定")]
    [SerializeField] private float _switchCooldown = 0.3f;

    [Header("プレイヤー参照")]
    [SerializeField] private Player _player; //　一時的に直接プレイヤー参照する。

    private CameraManager _cameraManager;
    private Transform _playerTransform;
    private Camera _mainCamera;
    private float _lastSwitchTime;
    private readonly Collider[] _overlapBuffer = new Collider[32];

    private System.Action _onLockOnLeft;
    private System.Action _onLockOnRight;

    private void Awake()
    {
        ServiceLocator.Register(this);
    }

    private void Start()
    {
        _cameraManager = ServiceLocator.Get<CameraManager>();
        _mainCamera = _cameraManager.MainCamera;
        _playerTransform = _player.transform;

        _onLockOnLeft = () => SwitchLockOnTarget(-1);
        _onLockOnRight = () => SwitchLockOnTarget(1);

        InputHandler inputHandler = ServiceLocator.Get<InputHandler>();
        inputHandler.OnLockOn += ToggleLockOn;
        inputHandler.OnLockOnLeft += _onLockOnLeft;
        inputHandler.OnLockOnRight += _onLockOnRight;
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<LockOnManager>();

        InputHandler inputHandler = ServiceLocator.Get<InputHandler>();
        if (inputHandler == null) return;

        inputHandler.OnLockOn -= ToggleLockOn;
        inputHandler.OnLockOnLeft -= _onLockOnLeft;
        inputHandler.OnLockOnRight -= _onLockOnRight;
    }
    private void ToggleLockOn()
    {
        if (_cameraManager.IsLockedOn)
        {
            _cameraManager.Unlock();
        }
        else
        {
            TryLockOn();
        }
    }

    private void TryLockOn()
    {
        ILockOnTarget best = FindCenterTarget();
        if (best == null) return;

        _cameraManager.LockOn(best);
    }

    /// <summary>
    /// 左右にロックオン対象を切り替える
    /// direction: -1=左 1=右
    /// </summary>
private void SwitchLockOnTarget(int direction)
{
    if (!_cameraManager.IsLockedOn) return;
    if (Time.time - _lastSwitchTime < _switchCooldown) return;

    Debug.Log($"切り替え方向: {direction}");

    ILockOnTarget current = _cameraManager.CurrentTarget;
    Vector2 currentScreenPos = _mainCamera.WorldToViewportPoint(current.LockOnPoint.position);

    Debug.Log($"現在の対象の画面座標: {currentScreenPos}");

    ILockOnTarget best = null;
    float bestDist = float.MaxValue;

    foreach (var candidate in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
    {
        if (candidate is not ILockOnTarget target) continue;
        if (!target.IsLockable) continue;
        if (target == current) continue;

        Vector3 screenPos = _mainCamera.WorldToViewportPoint(target.LockOnPoint.position);

        if (screenPos.x < 0f || screenPos.x > 1f ||
            screenPos.y < 0f || screenPos.y > 1f ||
            screenPos.z < 0f) continue;

        float diffX = screenPos.x - currentScreenPos.x;

        Debug.Log($"候補: {candidate.name} screenPos={screenPos} diffX={diffX}");

        if (direction == -1 && diffX >= 0f) continue;
        if (direction == 1 && diffX <= 0f) continue;

        float screenDist = Vector2.Distance(
            new Vector2(screenPos.x, screenPos.y),
            new Vector2(currentScreenPos.x, currentScreenPos.y)
        );

        if (screenDist < bestDist)
        {
            bestDist = screenDist;
            best = target;
        }
    }

    Debug.Log($"選ばれた対象: {(best != null ? best.LockOnPoint.gameObject.name : "なし")}");

    if (best == null) return;

    _cameraManager.LockOn(best);
    _lastSwitchTime = Time.time;
}

    /// <summary>
    /// 画面中心に最も近い敵を選ぶ（初回ロックオン用）
    /// </summary>
    private ILockOnTarget FindCenterTarget()
    {
        int hitCount = Physics.OverlapSphereNonAlloc(
            _playerTransform.position,
            _lockOnRange,
            _overlapBuffer,
            _lockOnLayer
        );

        ILockOnTarget best = null;
        float bestScore = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            if (!_overlapBuffer[i].TryGetComponent(out ILockOnTarget candidate)) continue;
            if (!candidate.IsLockable) continue;

            Vector3 screenPos = _mainCamera.WorldToViewportPoint(candidate.LockOnPoint.position);

            // 画面外は除外
            if (screenPos.x < 0f || screenPos.x > 1f ||
                screenPos.y < 0f || screenPos.y > 1f ||
                screenPos.z < 0f) continue;

            // 画面中心からの距離をスコアに
            float score = Vector2.Distance(
                new Vector2(screenPos.x, screenPos.y),
                new Vector2(0.5f, 0.5f)
            );

            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best;
    }
}