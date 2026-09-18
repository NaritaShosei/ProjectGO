using UnityEngine;

/// <summary>
/// 常時アクティブなプレイヤー追従カメラの状態です。
/// ロックオン対象がある間はその対象を追従し、ない間はスティック/マウス操作によるフリールックに戻ります。
/// </summary>
public sealed class FollowCameraState : ICameraState
{
    /// <summary>現在のロックオン対象を取得します。</summary>
    public ILockOnTarget Target { get; private set; }

    public FollowCameraState(CameraMotionController motionController, Camera mainCamera)
    {
        _motionController = motionController;
        _mainCamera = mainCamera;
    }

    public bool IsTargetValid => Target != null
        && Target.IsLockable
        && Target.GetTargetCenter() != null;

    /// <summary>ロックオン対象を設定し、ブレンドを開始する。</summary>
    /// <param name="isInitialLockOn">初回ロックオンなら true（現在表示中のカメラ姿勢から）、対象切り替えなら false（現在のカメラ姿勢から）。</param>
    public void SetTarget(ILockOnTarget target, bool isInitialLockOn)
    {
        Target = target;
        _motionController.EnterLockOn();
        _motionController.BeginLockOnBlend(snapFromCurrentCamera: isInitialLockOn, currentMainCamera: _mainCamera);
    }

    /// <summary>ロックオン対象を解除する。以後はフリールックに戻る。</summary>
    public void ClearTarget()
    {
        Target = null;
        _motionController.CancelLockOnBlend();
        _motionController.ExitLockOn();
    }

    /// <summary>画面座標の計算に使用するメインカメラを更新します。</summary>
    public void SetMainCamera(Camera mainCamera)
    {
        _mainCamera = mainCamera;
    }

    public void Enter()
    {
    }

    public void Tick(float timeScale, Vector2 cameraInput)
    {
        if (IsTargetValid)
        {
            _motionController.UpdateLockOn(_mainCamera, Target.GetTargetCenter());
        }
        else
        {
            _motionController.UpdateFreeLook(timeScale, cameraInput);
        }
    }

    public void Exit()
    {
    }

    private readonly CameraMotionController _motionController;
    private Camera _mainCamera;
}
