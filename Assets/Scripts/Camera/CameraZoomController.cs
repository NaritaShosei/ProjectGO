using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// 通常カメラ・ロックオンカメラ・ボスカメラ（任意）の視野角(FOV)をまとめて制御するズーム専用クラスです。
/// ズーム倍率1.0が通常視野、1未満でズームイン（画角が狭まる）、1より大きい値でズームアウト
/// （通常視野より画角が広がる）を表します。
/// 倍率はベース層とエフェクト層の2段で、実FOV = 基準FOV × ベース倍率 × エフェクト倍率。
/// ベース層（<see cref="SetBaseZoom"/>）はボスの姿勢連動など「基準そのものを動かす」用途、
/// エフェクト層（<see cref="SetZoom"/> 他）はチャージ・モード変更など「基準に対して一時的に掛ける」用途。
/// </summary>
public sealed class CameraZoomController
{
    /// <summary>現在適用されている実効ズーム倍率（ベース×エフェクト）を取得します。</summary>
    public float CurrentZoom => _baseCurrentZoom * _currentZoom;

    /// <summary>エフェクト層の補間先ズーム倍率を取得します。</summary>
    public float TargetZoom => _targetZoom;

    /// <summary>
    /// ズーム制御を初期化します。
    /// </summary>
    /// <param name="bossCamera">ボス戦用カメラ。未使用のシーンではnull可。</param>
    public CameraZoomController(
        CinemachineCamera normalCamera,
        CinemachineCamera lockOnCamera,
        CinemachineCamera bossCamera)
    {
        _normalCamera = normalCamera;
        _lockOnCamera = lockOnCamera;
        _bossCamera = bossCamera;
        _normalFieldOfView = normalCamera.Lens.FieldOfView;
        _lockOnFieldOfView = lockOnCamera.Lens.FieldOfView;
        _bossFieldOfView = bossCamera != null ? bossCamera.Lens.FieldOfView : 0f;
        _currentZoom = 1f;
        _targetZoom = 1f;
        _baseCurrentZoom = 1f;
        _baseTargetZoom = 1f;
    }

    /// <summary>
    /// ベース層のズーム倍率を設定します。エフェクト層とは独立に補間され、実FOVには両者の積が掛かります。
    /// ボスの姿勢連動など「基準そのものを動かす」用途に使い、チャージ等のエフェクト層はこの上に乗ります。
    /// </summary>
    public void SetBaseZoom(float zoom, float duration)
    {
        _baseStartValue = _baseCurrentZoom;
        _baseTargetZoom = Mathf.Max(0.01f, zoom);
        _baseDuration = Mathf.Max(0f, duration);
        _baseElapsed = 0f;
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

    /// <summary>ベース層・エフェクト層それぞれの目標倍率へ補間し、両者の積をカメラの視野角へ反映します。</summary>
    public void Tick(float deltaTime)
    {
        float dt = Mathf.Max(0f, deltaTime);

        // エフェクト層
        _zoomElapsed += dt;
        float t = _zoomDuration > 0f ? Mathf.Clamp01(_zoomElapsed / _zoomDuration) : 1f;
        _currentZoom = Mathf.Lerp(_zoomStartValue, _targetZoom, t);

        if (t >= 1f && _hasPendingSettle)
        {
            float settleZoom = _pendingSettleZoom;
            float settleDuration = _pendingSettleDuration;
            SetZoom(settleZoom, settleDuration);
        }

        // ベース層
        _baseElapsed += dt;
        float bt = _baseDuration > 0f ? Mathf.Clamp01(_baseElapsed / _baseDuration) : 1f;
        _baseCurrentZoom = Mathf.Lerp(_baseStartValue, _baseTargetZoom, bt);

        // 実FOV = 基準FOV × ベース倍率 × エフェクト倍率
        float applied = _baseCurrentZoom * _currentZoom;
        SetFieldOfView(_normalCamera, _normalFieldOfView * applied);
        SetFieldOfView(_lockOnCamera, _lockOnFieldOfView * applied);
        if (_bossCamera != null) SetFieldOfView(_bossCamera, _bossFieldOfView * applied);
    }

    private readonly CinemachineCamera _normalCamera;
    private readonly CinemachineCamera _lockOnCamera;
    private readonly CinemachineCamera _bossCamera;
    private readonly float _normalFieldOfView;
    private readonly float _lockOnFieldOfView;
    private readonly float _bossFieldOfView;

    // エフェクト層（チャージ・モード変更など、基準に一時的に掛ける）
    private float _currentZoom;
    private float _targetZoom;
    private float _zoomStartValue;
    private float _zoomDuration;
    private float _zoomElapsed;

    private bool _hasPendingSettle;
    private float _pendingSettleZoom;
    private float _pendingSettleDuration;

    // ベース層（ボスの姿勢連動など、基準そのものを動かす）
    private float _baseCurrentZoom;
    private float _baseTargetZoom;
    private float _baseStartValue;
    private float _baseDuration;
    private float _baseElapsed;

    private static void SetFieldOfView(CinemachineCamera camera, float fieldOfView)
    {
        LensSettings lens = camera.Lens;
        lens.FieldOfView = fieldOfView;
        camera.Lens = lens;
    }
}
