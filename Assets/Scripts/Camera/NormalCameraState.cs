using UnityEngine;

/// <summary>
/// 通常時のカメラ状態です。
/// </summary>
public sealed class NormalCameraState : ICameraState
{
    /// <summary>通常カメラ状態を初期化します。</summary>
    public NormalCameraState(CameraMotionController motionController)
    {
        _motionController = motionController;
    }

    /// <summary>通常カメラ状態へ入ります。</summary>
    public void Enter()
    {
    }

    /// <summary>通常カメラを更新します。</summary>
    public void Tick(float timeScale, Vector2 cameraInput)
    {
        _motionController.UpdateNormal(timeScale, cameraInput);
    }

    /// <summary>通常カメラ状態から退出します。</summary>
    public void Exit()
    {
    }

    private readonly CameraMotionController _motionController;
}