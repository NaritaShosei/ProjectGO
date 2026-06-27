using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// ジャスト回避成功時のヒットストップと画面 Vignette 演出を再生する。
/// Vignette は再生前の値を保存し、演出終了時に元の状態へ戻す。
/// </summary>
public class JustDodgeEffectPlayer : MonoBehaviour
{
    public void Play(JustDodgeContext context)
    {
        if (ServiceLocator.TryGet(out HitStopManager hitStopManager))
        {
            hitStopManager.Trigger(_hitStopData);
        }

        StopVignette(restore: true);

        _vignetteCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
        _vignettePlayVersion++;

        PlayVignette(_vignetteCts.Token, _vignettePlayVersion).Forget();
    }

    [Header("ヒットストップ設定")]
    [SerializeField] private HitStopData _hitStopData;

    [Header("ポストプロセス設定")]
    [SerializeField] private Volume _volume;
    [Tooltip("ジャスト回避直後の Vignette 強度")]
    [SerializeField, Range(0, 1)] private float _vignetteIntensity = 0.8f;
    [Tooltip("Vignette を最大値で維持する時間")]
    [SerializeField] private float _vignetteDuration = 1f;
    [Tooltip("Vignette を演出前の値へ戻す時間")]
    [SerializeField] private float _vignetteCloseDuration = 0.3f;
    [Tooltip("Vignette を戻す時の補間カーブ")]
    [SerializeField] private AnimationCurve _vignetteCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [Tooltip("ジャスト回避時の Vignette 色")]
    [SerializeField] private Color _vignetteColor = Color.skyBlue;

    private Vignette _vignette;
    private CancellationTokenSource _vignetteCts;
    private VignetteSnapshot _activeSnapshot;
    private bool _hasActiveSnapshot;
    private int _vignettePlayVersion;

    private void Awake()
    {
        // 未設定ならシーン内の Volume を探す。
        if (_volume == null)
            _volume = FindAnyObjectByType<Volume>();

        if (_volume == null)
        {
            Debug.LogWarning("[JustDodgeEffectPlayer] GlobalVolume is missing.");
            return;
        }

        if (!_volume.profile.TryGet(out _vignette))
            Debug.LogWarning("[JustDodgeEffectPlayer] GlobalVolume に Vignette が存在しません。");
    }

    private async UniTask PlayVignette(CancellationToken token, int playVersion)
    {
        if (_vignette == null) return;

        var snapshot = CaptureSnapshot();
        _activeSnapshot = snapshot;
        _hasActiveSnapshot = true;

        _vignette.color.overrideState = true;
        _vignette.intensity.overrideState = true;
        _vignette.color.value = _vignetteColor;
        _vignette.intensity.value = _vignetteIntensity;

        try
        {
            if (_vignetteDuration > 0f)
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(_vignetteDuration),
                    DelayType.UnscaledDeltaTime,
                    PlayerLoopTiming.Update,
                    token
                );
            }

            float elapsed = 0f;

            while (elapsed < _vignetteCloseDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(elapsed / _vignetteCloseDuration);
                float curveValue = _vignetteCurve.Evaluate(t);

                _vignette.intensity.value = Mathf.Lerp(
                    _vignetteIntensity,
                    snapshot.Intensity,
                    curveValue
                );

                await UniTask.Yield(token);
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
        finally
        {
            // 連続再生時、古い非同期処理が新しい演出の値を戻してしまうのを防ぐ。
            if (playVersion == _vignettePlayVersion)
                RestoreSnapshot(snapshot);
        }
    }

    private void OnDestroy()
    {
        StopVignette(restore: true);
    }

    private VignetteSnapshot CaptureSnapshot()
    {
        return new VignetteSnapshot
        {
            Intensity = _vignette.intensity.value,
            Color = _vignette.color.value,
            IntensityOverride = _vignette.intensity.overrideState,
            ColorOverride = _vignette.color.overrideState,
        };
    }

    private void RestoreSnapshot(VignetteSnapshot snapshot)
    {
        if (_vignette != null)
        {
            _vignette.intensity.value = snapshot.Intensity;
            _vignette.color.value = snapshot.Color;
            _vignette.intensity.overrideState = snapshot.IntensityOverride;
            _vignette.color.overrideState = snapshot.ColorOverride;
        }

        _hasActiveSnapshot = false;
    }

    private void StopVignette(bool restore)
    {
        _vignetteCts?.Cancel();
        _vignetteCts?.Dispose();
        _vignetteCts = null;

        if (restore && _hasActiveSnapshot)
            RestoreSnapshot(_activeSnapshot);
    }

    private struct VignetteSnapshot
    {
        public float Intensity;
        public Color Color;
        public bool IntensityOverride;
        public bool ColorOverride;
    }
}

public struct JustDodgeContext
{
    // KISSの原則には反しているが許して
    // 今後必要になるかもしれないので作った。
    // 不必要なら削除する
}
