using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// 通常カメラとロックオンカメラの視野角(FOV)をまとめて制御するズーム専用クラスです。
/// ズーム倍率1.0が通常視野、1未満でズームイン（画角が狭まる）、1より大きい値でズームアウト
/// （通常視野より画角が広がる）を表します。
/// </summary>
public sealed class CameraZoomController
{
    /// <summary>現在適用されているズーム倍率を取得します。</summary>
    public float CurrentZoom => _currentZoom;

    /// <summary>補間先のズーム倍率を取得します。</summary>
    public float TargetZoom => _targetZoom;

    /// <summary>
    /// ズーム制御を初期化します。
    /// </summary>
    public CameraZoomController(
        CinemachineCamera normalCamera,
        CinemachineCamera lockOnCamera)
    {
        _normalCamera = normalCamera;
        _lockOnCamera = lockOnCamera;
        _normalFieldOfView = normalCamera.Lens.FieldOfView;
        _lockOnFieldOfView = lockOnCamera.Lens.FieldOfView;
        _currentZoom = 1f;
        _targetZoom = 1f;
    }

    /// <summary>
    /// ズーム倍率を設定します。1.0が通常視野、1未満でズームイン、1より大きい値でズームアウトです。
    /// 呼び出し時点の現在値から、移動距離に関わらず必ずduration秒かけて到達します。
    /// </summary>
    public void SetZoom(float zoom, float duration)
    {
        _hasPendingSettle = false;
        _zoomStartValue = _currentZoom;
        _targetZoom = Mathf.Max(0.01f, zoom);
        _zoomDuration = Mathf.Max(0f, duration);
        _zoomElapsed = 0f;
    }

    /// <summary>
    /// まずovershootZoomへovershootDuration秒かけて遷移し、到達したら続けて
    /// settleZoomへsettleDuration秒かけて遷移する2段階のズームを開始します。
    /// </summary>
    public void SetZoomSequence(float overshootZoom, float overshootDuration, float settleZoom, float settleDuration)
    {
        SetZoom(overshootZoom, overshootDuration);
        _pendingSettleZoom = settleZoom;
        _pendingSettleDuration = settleDuration;
        _hasPendingSettle = true;
    }

    /// <summary>指定量だけズームイン方向（倍率を下げる方向）へ変化させます。</summary>
    public void ZoomIn(float amount, float duration)
    {
        SetZoom(_targetZoom - amount, duration);
    }

    /// <summary>指定量だけズームアウト方向（倍率を上げる方向）へ変化させます。</summary>
    public void ZoomOut(float amount, float duration)
    {
        SetZoom(_targetZoom + amount, duration);
    }

    /// <summary>ズームを通常視野（倍率1.0）へ戻します。</summary>
    public void ResetZoom(float duration = 0f)
    {
        SetZoom(1f, duration);
    }

    /// <summary>現在の目標ズーム倍率へカメラの視野角を補間します。</summary>
    public void Tick(float deltaTime)
    {
        _zoomElapsed += Mathf.Max(0f, deltaTime);
        float t = _zoomDuration > 0f ? Mathf.Clamp01(_zoomElapsed / _zoomDuration) : 1f;
        _currentZoom = Mathf.Lerp(_zoomStartValue, _targetZoom, t);

        if (t >= 1f && _hasPendingSettle)
        {
            float settleZoom = _pendingSettleZoom;
            float settleDuration = _pendingSettleDuration;
            SetZoom(settleZoom, settleDuration);
        }

        // 基準FOVに倍率を直接掛けるだけなので、Lerpのような補間トリックは不要。
        SetFieldOfView(_normalCamera, _normalFieldOfView * _currentZoom);
        SetFieldOfView(_lockOnCamera, _lockOnFieldOfView * _currentZoom);
    }

    private readonly CinemachineCamera _normalCamera;
    private readonly CinemachineCamera _lockOnCamera;
    private readonly float _normalFieldOfView;
    private readonly float _lockOnFieldOfView;

    private float _currentZoom;
    private float _targetZoom;
    private float _zoomStartValue;
    private float _zoomDuration;
    private float _zoomElapsed;

    private bool _hasPendingSettle;
    private float _pendingSettleZoom;
    private float _pendingSettleDuration;

    private static void SetFieldOfView(CinemachineCamera camera, float fieldOfView)
    {
        LensSettings lens = camera.Lens;
        lens.FieldOfView = fieldOfView;
        camera.Lens = lens;
    }
}
