using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Warrior から Thunder へ切り替わる瞬間だけ、画面全体のポストプロセス演出を再生する。
/// モード変更ロジックには触らず、PlayerModeController のイベントを購読して演出だけを担当する。
/// </summary>
public class ModeChangePostProcessEffectPlayer : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private PlayerModeController _modeController;
    [Tooltip("未設定の場合はシーン内の Volume を自動取得します。")]
    [SerializeField] private Volume _volume;

    [Header("時間設定")]
    [Tooltip("演出全体の再生時間")]
    [SerializeField, Min(0.01f)] private float _duration = 1.1f;
    [Tooltip("演出の強さの時間変化")]
    [SerializeField] private AnimationCurve _impactCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.18f, 1f),
        new Keyframe(0.42f, 0.55f),
        new Keyframe(1f, 0f));

    [Header("見た目")]
    [Tooltip("雷神モード突入時の Vignette 色")]
    [SerializeField] private Color _vignetteColor = new Color(0.25f, 0.75f, 1f);
    [Tooltip("演出ピーク時の Vignette 強度")]
    [SerializeField, Range(0f, 1f)] private float _vignetteIntensity = 0.48f;
    [Tooltip("演出ピーク時の色収差の強度")]
    [SerializeField, Range(0f, 1f)] private float _chromaticAberrationIntensity = 0.75f;
    [Tooltip("演出ピーク時のレンズ歪み。負数にすると中心へ吸い込む印象になる")]
    [SerializeField, Range(-1f, 1f)] private float _lensDistortionIntensity = -0.28f;
    [Tooltip("現在の Bloom 強度に加算する値")]
    [SerializeField, Min(0f)] private float _bloomIntensityBoost = 6f;
    [Tooltip("現在の露出に加算する値")]
    [SerializeField] private float _postExposureBoost = 1.2f;
    [Tooltip("現在の彩度に加算する値")]
    [SerializeField] private float _saturationBoost = 35f;

    private PlayerMode _previousMode = PlayerMode.Warrior;
    private Vignette _vignette;
    private ChromaticAberration _chromaticAberration;
    private LensDistortion _lensDistortion;
    private Bloom _bloom;
    private ColorAdjustments _colorAdjustments;
    private CancellationTokenSource _effectCts;
    private PostProcessSnapshot _activeSnapshot;
    private bool _hasActiveSnapshot;
    private int _effectPlayVersion;

    private void Awake()
    {
        if (_modeController == null)
            _modeController = GetComponent<PlayerModeController>();

        if (_volume == null)
            _volume = FindAnyObjectByType<Volume>();

        if (_modeController != null)
            _previousMode = _modeController.CurrentMode;

        CacheVolumeComponents();
    }

    private void OnEnable()
    {
        if (_modeController != null)
            _modeController.OnModeChanged += HandleModeChanged;
    }

    private void OnDisable()
    {
        if (_modeController != null)
            _modeController.OnModeChanged -= HandleModeChanged;

        StopEffect(restore: true);
    }

    private void HandleModeChanged(PlayerMode newMode)
    {
        // OnModeChanged は遷移後のモードだけを通知するため、このクラス側で直前のモードを保持する。
        bool isWarriorToThunder = _previousMode == PlayerMode.Warrior
            && newMode == PlayerMode.Thunder;

        _previousMode = newMode;

        if (!isWarriorToThunder) return;

        // 前回演出の停止と復元を先に完了させてから、次のスナップショットを取る。
        StopEffect(restore: true);

        _effectCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
        _effectPlayVersion++;

        PlayEffect(_effectCts.Token, _effectPlayVersion).Forget();
    }

    private void CacheVolumeComponents()
    {
        if (_volume == null)
        {
            Debug.LogWarning("[ModeChangePostProcessEffectPlayer] Volume が存在しません。", this);
            return;
        }

        if (_volume.profile == null)
        {
            Debug.LogWarning("[ModeChangePostProcessEffectPlayer] Volume Profile が存在しません。", this);
            return;
        }

        // Profile に存在するコンポーネントだけを操作する。
        _volume.profile.TryGet(out _vignette);
        _volume.profile.TryGet(out _chromaticAberration);
        _volume.profile.TryGet(out _lensDistortion);
        _volume.profile.TryGet(out _bloom);
        _volume.profile.TryGet(out _colorAdjustments);
    }

    private async UniTask PlayEffect(CancellationToken token, int playVersion)
    {
        if (_volume == null || _volume.profile == null) return;

        var snapshot = CaptureSnapshot();
        _activeSnapshot = snapshot;
        _hasActiveSnapshot = true;

        SetOverrideState(true);

        float elapsed = 0f;

        try
        {
            while (elapsed < _duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / _duration);
                float strength = Mathf.Clamp01(_impactCurve.Evaluate(normalizedTime));

                Apply(strength, snapshot);

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
        finally
        {
            // 連続再生時、古い非同期処理が新しい演出の値を戻してしまうのを防ぐ。
            if (playVersion == _effectPlayVersion)
                Restore(snapshot);
        }
    }

    private PostProcessSnapshot CaptureSnapshot()
    {
        return new PostProcessSnapshot
        {
            VignetteIntensity = _vignette != null ? _vignette.intensity.value : 0f,
            VignetteColor = _vignette != null ? _vignette.color.value : Color.black,
            VignetteIntensityOverride = _vignette != null && _vignette.intensity.overrideState,
            VignetteColorOverride = _vignette != null && _vignette.color.overrideState,

            ChromaticAberrationIntensity = _chromaticAberration != null ? _chromaticAberration.intensity.value : 0f,
            ChromaticAberrationOverride = _chromaticAberration != null && _chromaticAberration.intensity.overrideState,

            LensDistortionIntensity = _lensDistortion != null ? _lensDistortion.intensity.value : 0f,
            LensDistortionOverride = _lensDistortion != null && _lensDistortion.intensity.overrideState,

            BloomIntensity = _bloom != null ? _bloom.intensity.value : 0f,
            BloomOverride = _bloom != null && _bloom.intensity.overrideState,

            PostExposure = _colorAdjustments != null ? _colorAdjustments.postExposure.value : 0f,
            Saturation = _colorAdjustments != null ? _colorAdjustments.saturation.value : 0f,
            PostExposureOverride = _colorAdjustments != null && _colorAdjustments.postExposure.overrideState,
            SaturationOverride = _colorAdjustments != null && _colorAdjustments.saturation.overrideState,
        };
    }

    private void SetOverrideState(bool enabled)
    {
        if (_vignette != null)
        {
            _vignette.intensity.overrideState = enabled;
            _vignette.color.overrideState = enabled;
        }

        if (_chromaticAberration != null)
            _chromaticAberration.intensity.overrideState = enabled;

        if (_lensDistortion != null)
            _lensDistortion.intensity.overrideState = enabled;

        if (_bloom != null)
            _bloom.intensity.overrideState = enabled;

        if (_colorAdjustments != null)
        {
            _colorAdjustments.postExposure.overrideState = enabled;
            _colorAdjustments.saturation.overrideState = enabled;
        }
    }

    private void Apply(float strength, PostProcessSnapshot snapshot)
    {
        if (_vignette != null)
        {
            _vignette.intensity.value = Mathf.Lerp(snapshot.VignetteIntensity, _vignetteIntensity, strength);
            _vignette.color.value = Color.Lerp(snapshot.VignetteColor, _vignetteColor, strength);
        }

        if (_chromaticAberration != null)
        {
            _chromaticAberration.intensity.value = Mathf.Lerp(
                snapshot.ChromaticAberrationIntensity,
                _chromaticAberrationIntensity,
                strength);
        }

        if (_lensDistortion != null)
        {
            _lensDistortion.intensity.value = Mathf.Lerp(
                snapshot.LensDistortionIntensity,
                _lensDistortionIntensity,
                strength);
        }

        if (_bloom != null)
            _bloom.intensity.value = snapshot.BloomIntensity + _bloomIntensityBoost * strength;

        if (_colorAdjustments != null)
        {
            _colorAdjustments.postExposure.value = snapshot.PostExposure + _postExposureBoost * strength;
            _colorAdjustments.saturation.value = snapshot.Saturation + _saturationBoost * strength;
        }
    }

    private void Restore(PostProcessSnapshot snapshot)
    {
        if (_vignette != null)
        {
            _vignette.intensity.value = snapshot.VignetteIntensity;
            _vignette.color.value = snapshot.VignetteColor;
            _vignette.intensity.overrideState = snapshot.VignetteIntensityOverride;
            _vignette.color.overrideState = snapshot.VignetteColorOverride;
        }

        if (_chromaticAberration != null)
        {
            _chromaticAberration.intensity.value = snapshot.ChromaticAberrationIntensity;
            _chromaticAberration.intensity.overrideState = snapshot.ChromaticAberrationOverride;
        }

        if (_lensDistortion != null)
        {
            _lensDistortion.intensity.value = snapshot.LensDistortionIntensity;
            _lensDistortion.intensity.overrideState = snapshot.LensDistortionOverride;
        }

        if (_bloom != null)
        {
            _bloom.intensity.value = snapshot.BloomIntensity;
            _bloom.intensity.overrideState = snapshot.BloomOverride;
        }

        if (_colorAdjustments != null)
        {
            _colorAdjustments.postExposure.value = snapshot.PostExposure;
            _colorAdjustments.saturation.value = snapshot.Saturation;
            _colorAdjustments.postExposure.overrideState = snapshot.PostExposureOverride;
            _colorAdjustments.saturation.overrideState = snapshot.SaturationOverride;
        }

        _hasActiveSnapshot = false;
    }

    private void StopEffect(bool restore)
    {
        _effectCts?.Cancel();
        _effectCts?.Dispose();
        _effectCts = null;

        if (restore && _hasActiveSnapshot)
            Restore(_activeSnapshot);
    }

    private struct PostProcessSnapshot
    {
        public float VignetteIntensity;
        public Color VignetteColor;
        public bool VignetteIntensityOverride;
        public bool VignetteColorOverride;
        public float ChromaticAberrationIntensity;
        public bool ChromaticAberrationOverride;
        public float LensDistortionIntensity;
        public bool LensDistortionOverride;
        public float BloomIntensity;
        public bool BloomOverride;
        public float PostExposure;
        public float Saturation;
        public bool PostExposureOverride;
        public bool SaturationOverride;
    }
}
