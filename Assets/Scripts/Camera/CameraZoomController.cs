using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// 通常カメラとロックオンカメラのズーム量を一元管理します。
/// ズーム率0が通常視野、1が最大ズームです。
/// </summary>
public sealed class CameraZoomController
{
    /// <summary>現在適用されているズーム率を取得します。</summary>
    public float CurrentZoom => _currentZoom;

    /// <summary>補間先のズーム率を取得します。</summary>
    public float TargetZoom => _targetZoom;

    /// <summary>
    /// ズーム制御を初期化します。
    /// </summary>
    public CameraZoomController(
        CinemachineCamera normalCamera,
        CinemachineCamera lockOnCamera,
        float minFieldOfView,
        float zoomSpeed)
    {
        _normalCamera = normalCamera;
        _lockOnCamera = lockOnCamera;
        _normalFieldOfView = normalCamera.Lens.FieldOfView;
        _lockOnFieldOfView = lockOnCamera.Lens.FieldOfView;
        minFieldOfView = Mathf.Max(1f, minFieldOfView);
        _normalMinFieldOfView = Mathf.Min(_normalFieldOfView, minFieldOfView);
        _lockOnMinFieldOfView = Mathf.Min(_lockOnFieldOfView, minFieldOfView);
        _zoomSpeed = Mathf.Max(0f, zoomSpeed);
    }

    /// <summary>ズーム率を設定します。0は通常視野、1は最大ズームです。</summary>
    public void SetZoom(float zoom)
    {
        _targetZoom = Mathf.Clamp01(zoom);
    }

    /// <summary>指定量だけズームインします。</summary>
    public void ZoomIn(float amount)
    {
        SetZoom(_targetZoom + amount);
    }

    /// <summary>指定量だけズームアウトします。</summary>
    public void ZoomOut(float amount)
    {
        SetZoom(_targetZoom - amount);
    }

    /// <summary>ズームを通常視野へ戻します。</summary>
    public void ResetZoom()
    {
        SetZoom(0f);
    }

    /// <summary>現在の目標ズーム率へカメラの視野角を補間します。</summary>
    public void Tick(float deltaTime)
    {
        _currentZoom = Mathf.MoveTowards(
            _currentZoom,
            _targetZoom,
            _zoomSpeed * Mathf.Max(0f, deltaTime));

        float normalFieldOfView = Mathf.Lerp(
            _normalFieldOfView,
            _normalMinFieldOfView,
            _currentZoom);
        float lockOnFieldOfView = Mathf.Lerp(
            _lockOnFieldOfView,
            _lockOnMinFieldOfView,
            _currentZoom);

        SetFieldOfView(_normalCamera, normalFieldOfView);
        SetFieldOfView(_lockOnCamera, lockOnFieldOfView);
    }

    private readonly CinemachineCamera _normalCamera;
    private readonly CinemachineCamera _lockOnCamera;
    private readonly float _normalFieldOfView;
    private readonly float _lockOnFieldOfView;
    private readonly float _normalMinFieldOfView;
    private readonly float _lockOnMinFieldOfView;
    private readonly float _zoomSpeed;

    private float _currentZoom;
    private float _targetZoom;

    private static void SetFieldOfView(CinemachineCamera camera, float fieldOfView)
    {
        LensSettings lens = camera.Lens;
        lens.FieldOfView = fieldOfView;
        camera.Lens = lens;
    }
}