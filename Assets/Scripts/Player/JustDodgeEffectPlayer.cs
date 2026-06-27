using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class JustDodgeEffectPlayer : MonoBehaviour
{
    public void Play(JustDodgeContext context)
    {
        if (ServiceLocator.TryGet(out HitStopManager hitStopManager))
        {
            hitStopManager.Trigger(_hitStopData);
        }

        _vignetteCts?.Cancel();
        _vignetteCts?.Dispose();
        _vignetteCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);

        PlayVignette(_vignetteCts.Token).Forget();
    }

    [Header("ヒットストップ設定")]
    [SerializeField] private HitStopData _hitStopData;
    [Header("ポストプロセス設定")]
    [SerializeField] private Volume _volume;
    [SerializeField, Range(0, 1)] private float _vignetteMin = 0.116f;
    [SerializeField, Range(0, 1)] private float _vignetteMax = 0.8f;
    [SerializeField] private float _vignetteDuration = 1f;
    [SerializeField] private float _vignetteCloseDuration = 0.3f;
    [SerializeField] private AnimationCurve _vignetteCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField] private Color _defaultVignetteColor = Color.black;
    [SerializeField] private Color _vignetteColor = Color.skyBlue;

    private Vignette _vignette;
    private CancellationTokenSource _vignetteCts;

    private void Awake()
    {
        // nullだったときに探す
        if (_volume == null)
            _volume = FindAnyObjectByType<Volume>();

        if (_volume == null)
        {
            Debug.LogWarning("[JustDodgeEffectPlayer] GlobalVolume がシーンに存在しません");
            return;
        }

        if (!_volume.profile.TryGet(out _vignette))
            Debug.LogWarning("[JustDodgeEffectPlayer] GlobalVolume に Vignette が存在しません");
    }

    private async UniTask PlayVignette(CancellationToken token)
    {
        if (_vignette == null) return;

        _vignette.color.value = _vignetteColor;
        _vignette.intensity.value = _vignetteMax;

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
                    _vignetteMax,
                    _vignetteMin,
                    curveValue
                );

                await UniTask.Yield(token);
            }

            _vignette.intensity.value = _vignetteMin;
            _vignette.color.value = _defaultVignetteColor;
        }
        catch (OperationCanceledException)
        {
            return;
        }
    }

    private void OnDestroy()
    {
        _vignetteCts?.Cancel();
        _vignetteCts?.Dispose();
        _vignetteCts = null;
    }
}

public struct JustDodgeContext
{
    // KISSの原則には反しているが許して
    // 今後必要になるかもしれないので作った。
    // 不必要なら削除する
}
