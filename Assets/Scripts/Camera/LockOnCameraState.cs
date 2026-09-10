using UnityEngine;

/// <summary>
/// ロックオン中のカメラ状態です。
/// </summary>
public sealed class LockOnCameraState : ICameraState
{
    /// <summary>現在のロックオン対象を取得します。</summary>
    public ILockOnTarget Target { get; private set; }

    public LockOnCameraState(
        CameraMotionController motionController,
        Camera mainCamera,
        Transform playerTransform,
        float autoUnlockRange)
    {
        _motionController = motionController;
        _mainCamera = mainCamera;
        _playerTransform = playerTransform;
        _autoUnlockRange = autoUnlockRange;
    }

    public bool IsTargetValid => Target != null
        && Target.IsLockable
        && Target.GetTargetCenter() != null;

    public bool IsTargetOutOfRange => IsTargetValid
        && Vector3.Distance(
            _playerTransform.position,
            Target.GetTargetCenter().position) > _autoUnlockRange;

    public void SetTarget(ILockOnTarget target)
    {
        Target = target;
    }

    /// <summary>画面座標の計算に使用するメインカメラを更新します。</summary>
    public void SetMainCamera(Camera mainCamera)
    {
        _mainCamera = mainCamera;
    }

    public void Enter()
    {
        _motionController.BeginLockOnBlend();
    }

    public void Tick(float timeScale, Vector2 cameraInput)
    {
        if (!IsTargetValid) return;

        _motionController.UpdateLockOn(
            _mainCamera,
            Target.GetTargetCenter());
    }

    public void Exit()
    {
        _motionController.ApplyRotationToNormalCamera();
        _motionController.CancelLockOnBlend();
        Target = null;
    }

    private readonly CameraMotionController _motionController;
    private readonly Transform _playerTransform;
    private readonly float _autoUnlockRange;
    private Camera _mainCamera;
}
