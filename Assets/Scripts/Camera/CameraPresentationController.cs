using System;
using Cysharp.Threading.Tasks;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>チャージ段階到達時のFOV倍率と到達時間の組。</summary>
[Serializable]
public struct ChargeZoomSetting
{
    [Tooltip("到達するFOVの倍率。1で変化なし、0.7なら通常視野の70%まで狭める（ズームイン）")]
    public float Multiplier;
    [Tooltip("到達するまでの時間（秒）")]
    public float Duration;
}

/// <summary>
/// チャージ解放時、通常視野を超えて一瞬ズームアウト（オーバーシュート）してから
/// 通常視野へ戻るまでの設定。
/// </summary>
[Serializable]
public struct ReleaseZoomSetting
{
    [Tooltip("解放時に一瞬広げるFOVの倍率。1より大きい値。例: 1.1なら通常視野の110%まで広げる")]
    public float OvershootMultiplier;
    [Tooltip("オーバーシュートに到達するまでの時間（秒）")]
    public float OvershootDuration;
    [Tooltip("オーバーシュート後、通常視野（1.0倍）へ戻るまでの時間（秒）")]
    public float SettleDuration;
}

/// <summary>雷神モードへ切り替わった瞬間に一瞬ズームインしてから通常視野へ戻るまでの設定。</summary>
[Serializable]
public struct ModeChangeZoomSetting
{
    [Tooltip("雷神モードへ切り替わった瞬間に一瞬ズームインするFOVの倍率（1未満）")]
    public float Multiplier;
    [Tooltip("ズームインに到達するまでの時間（秒）")]
    public float ZoomInDuration;
    [Tooltip("ズームイン後、通常視野（1.0倍）へ戻るまでの時間（秒）")]
    public float ZoomOutDuration;
}

/// <summary>
/// ゲームイベント（チャージ、モード変更など）を受けてカメラの演出（ズーム・カメラシェイク）を
/// 発火する専用クラスです。カメラの参照・Priority・ライフサイクル管理は `CameraManager` が担当し、
/// このクラスは演出の発火条件と内容だけを持ちます。
/// </summary>
public sealed class CameraPresentationController
{
    /// <summary>現在のズームFOV倍率。1で通常視野、1未満でズームイン、1より大きい値でズームアウト。</summary>
    public float CurrentZoom => _zoomController?.CurrentZoom ?? 1f;

    public CameraPresentationController(
        CinemachineCamera normalCamera,
        CinemachineCamera lockOnCamera,
        PlayerAttack playerAttack,
        PlayerModeController playerModeController,
        ChargeZoomSetting level2Zoom,
        ChargeZoomSetting level3Zoom,
        ReleaseZoomSetting releaseZoom,
        ModeChangeZoomSetting thunderModeZoom)
    {
        _zoomController = new CameraZoomController(normalCamera, lockOnCamera);
        _cameraShake = new CameraShake();

        _level2Zoom = level2Zoom;
        _level3Zoom = level3Zoom;
        _releaseZoom = releaseZoom;
        _thunderModeZoom = thunderModeZoom;

        _playerAttack = playerAttack;
        if (_playerAttack != null)
        {
            _playerAttack.OnChargeLevelReached += HandleChargeLevelReached;
            _playerAttack.OnChargingEnded += HandleChargingEnded;
        }

        _playerModeController = playerModeController;
        if (_playerModeController != null)
        {
            _playerModeController.OnModeChanged += HandleModeChanged;
        }
    }

    /// <summary>ズーム倍率を目標値へ向けて補間します。</summary>
    public void Tick(float deltaTime)
    {
        _zoomController?.Tick(deltaTime);
    }

    /// <summary>
    /// ズームのFOV倍率を設定します。1は変化なし、1未満でズームイン、1より大きい値でズームアウトです。
    /// 現在値からの移動距離に関わらず、必ずduration秒かけて到達します。
    /// </summary>
    public void SetZoom(float zoom, float duration)
    {
        _zoomController?.SetZoom(zoom, duration);
    }

    /// <summary>
    /// チャージ段階をFOV倍率へ変換して設定します。
    /// 各段階の倍率・到達時間はコンストラクタで受け取った設定値に従います。
    /// Level1はズームなしのため何もしません。
    /// </summary>
    /// <returns>実際にズーム値を変更した場合はtrue。</returns>
    public bool SetZoomLevel(ChargeLevel level)
    {
        switch (level)
        {
            case ChargeLevel.Level2:
                SetZoom(_level2Zoom.Multiplier, _level2Zoom.Duration);
                return true;
            case ChargeLevel.Level3:
                SetZoom(_level3Zoom.Multiplier, _level3Zoom.Duration);
                return true;
            default:
                return false;
        }
    }

    /// <summary>指定量だけズームインします（倍率を下げます）。</summary>
    public void ZoomIn(float amount, float duration)
    {
        _zoomController?.ZoomIn(amount, duration);
    }

    /// <summary>指定量だけズームアウトします（倍率を上げます）。</summary>
    public void ZoomOut(float amount, float duration)
    {
        _zoomController?.ZoomOut(amount, duration);
    }

    /// <summary>ズームを通常視野（倍率1.0）へ戻します。</summary>
    public void ResetZoom(float duration = 0f)
    {
        _zoomController?.ResetZoom(duration);
    }

    /// <summary>指定したカメラでカメラシェイクを実行します。</summary>
    public async UniTask Shake(CinemachineCamera camera, CameraShakeData data)
    {
        await _cameraShake.StartCameraShake(camera, data);
    }

    /// <summary>カメラシェイクを強制停止します。</summary>
    public void ForceStopShake()
    {
        _cameraShake.ForceStopCameraShake();
    }

    /// <summary>購読していたイベントを解除します。</summary>
    public void Dispose()
    {
        if (_playerAttack != null)
        {
            _playerAttack.OnChargeLevelReached -= HandleChargeLevelReached;
            _playerAttack.OnChargingEnded -= HandleChargingEnded;
        }

        if (_playerModeController != null)
        {
            _playerModeController.OnModeChanged -= HandleModeChanged;
        }
    }

    private readonly CameraZoomController _zoomController;
    private readonly CameraShake _cameraShake;
    private readonly ChargeZoomSetting _level2Zoom;
    private readonly ChargeZoomSetting _level3Zoom;
    private readonly ReleaseZoomSetting _releaseZoom;
    private readonly ModeChangeZoomSetting _thunderModeZoom;
    private readonly PlayerAttack _playerAttack;
    private readonly PlayerModeController _playerModeController;

    private bool _hasChargedZoom;

    /// <summary>チャージ段階の通知を受けてズーム倍率を変更します。実際にズームした段階のみ解放時の演出対象とします。</summary>
    private void HandleChargeLevelReached(ChargeLevel level)
    {
        Debug.Log($"[CameraPresentationController] ChargeLevel : {level}");
        if (SetZoomLevel(level))
            _hasChargedZoom = true;
    }

    /// <summary>
    /// チャージ解放（攻撃発動 or キャンセル）を受けて、その時点のズーム倍率から
    /// 一旦通常視野を超えてズームアウトし、その後通常視野へ戻ります。
    /// 遷移は常にその時点の実際のズーム倍率を起点に行われるため、攻撃発動タイミングと自然に同期します。
    /// 実際にLevel2以上へズームしていた場合のみ発動し、単押しや未チャージの攻撃では発動しません。
    /// </summary>
    private void HandleChargingEnded()
    {
        if (!_hasChargedZoom) return;
        _hasChargedZoom = false;

        _zoomController?.SetZoomSequence(
            _releaseZoom.OvershootMultiplier, _releaseZoom.OvershootDuration,
            1f, _releaseZoom.SettleDuration);
    }

    /// <summary>
    /// モード変更の通知を受けて、雷神モードへ切り替わった瞬間のみ一瞬ズームインしてから
    /// 通常視野へ戻る演出を行います。
    /// </summary>
    private void HandleModeChanged(PlayerMode mode)
    {
        if (mode != PlayerMode.Thunder) return;

        _zoomController?.SetZoomSequence(
            _thunderModeZoom.Multiplier, _thunderModeZoom.ZoomInDuration,
            1f, _thunderModeZoom.ZoomOutDuration);
    }
}
