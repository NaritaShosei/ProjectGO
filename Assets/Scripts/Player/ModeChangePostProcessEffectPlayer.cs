using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 画面全体のモード切替ポストプロセス演出を再生するView。
/// 再生条件とモード変更イベントの購読は ModeChangePostProcessEffectPresenter が担当する。
/// </summary>
public class ModeChangePostProcessEffectPlayer : MonoBehaviour
{
    public event Action OnEffectEnabled;

    public async UniTaskVoid Play()
    {
        // 前回演出の停止と復元を先に完了させてから、次のスナップショットを取る。
        StopEffect(restore: true);

        _effectCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
        _effectPlayVersion++;

        PlayEffect(_effectCts.Token, _effectPlayVersion).Forget();

        // モーションに合わせてプレイヤー位置へモード変更エフェクトを生成するため遅延を行う。
        if (ServiceLocator.TryGet(out EffectManager effectManager))
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_spawnDelay), cancellationToken: _effectCts.Token);
                var playerTransform = transform;
                var effectPosition = playerTransform.position + _effectOffset;
                effectManager.PlayEffect(_effectName, effectPosition);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ModeChangePostProcessEffectPlayer] エフェクト再生の遅延中に例外が発生しました: {ex}", this);
                return;
            }
        }
    }

    public void Stop()
    {
        // キャンセルされた古い演出が finally でスナップショットを再適用しないよう、先に世代を無効化する。
        _effectPlayVersion++;
        StopEffect(restore: true);
        StopEmissionChange();
    }

    /// <summary>
    /// ハンマーの Emission Map Intensity を対象モードの設定値へ遷移させる。
    /// </summary>
    public void ChangeHammerEmission(PlayerMode mode, bool immediate = false)
    {
        if (!InitializeHammerEmission()) return;

        StopEmissionChange();

        float targetIntensity = mode == PlayerMode.Thunder
            ? _thunderEmissionIntensity
            : _warriorEmissionIntensity;
        Color targetColor = _hammerEmissionBaseColor * Mathf.Pow(2f, targetIntensity);

        if (immediate || _changeDuration <= 0f)
        {
            ApplyHammerEmissionColor(targetColor);
            return;
        }

        _emissionCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
        ChangeHammerEmissionAsync(targetColor, _emissionCts.Token).Forget();
    }

    [Header("Volume")]
    [Tooltip("未設定の場合はシーン内の Volume を自動取得します。")]
    [SerializeField] private Volume _volume;

    [Header("時間設定")]
    [Tooltip("演出全体の再生時間")]
    [SerializeField, Min(0.01f)] private float _duration = 1.1f;
    [Tooltip("演出の強さの時間変化")]
    [SerializeField]
    private AnimationCurve _impactCurve = new AnimationCurve(
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

    [Header("エフェクト設定")]
    [SerializeField] private string _effectName = "ModeChangeLightning";
    [SerializeField] private Vector3 _effectOffset = new Vector3(0f, 2f, 0f);
    [Tooltip("エフェクト再生までの遅延時間(秒)。演出のピークに合わせる")]
    [SerializeField] private float _spawnDelay = 0.4f;

    [Header("マテリアル設定")]
    [SerializeField] private Renderer _hammerRenderer;
    [SerializeField, Min(0f)] private float _changeDuration = 0.5f;
    [Tooltip("闘神モード時の Emission Map Intensity")]
    [SerializeField] private float _warriorEmissionIntensity = 1f;
    [Tooltip("雷神モード時の Emission Map Intensity")]
    [SerializeField] private float _thunderEmissionIntensity = 4f;

    private Vignette _vignette;
    private ChromaticAberration _chromaticAberration;
    private LensDistortion _lensDistortion;
    private Bloom _bloom;
    private ColorAdjustments _colorAdjustments;
    private CancellationTokenSource _effectCts;
    private CancellationTokenSource _emissionCts;
    private MaterialPropertyBlock _hammerPropertyBlock;
    private Color _hammerEmissionBaseColor = Color.white;
    private Color _currentHammerEmissionColor;
    private bool _isHammerEmissionInitialized;
    private PostProcessSnapshot _activeSnapshot;
    private bool _hasActiveSnapshot;
    private int _effectPlayVersion;
    private static readonly int _emissionColorId = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        if (_volume == null)
            _volume = FindAnyObjectByType<Volume>();

        CacheVolumeComponents();
        InitializeHammerEmission();
    }

    private void OnEnable()
    {
        // Presenter側で、無効中に変更された現在モードへ見た目を同期する。
        OnEffectEnabled?.Invoke();
    }

    private void OnDisable()
    {
        Stop();
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

    private bool InitializeHammerEmission()
    {
        if (_isHammerEmissionInitialized) return true;
        if (_hammerRenderer == null) return false;

        Material material = _hammerRenderer.sharedMaterial;
        if (material == null || !material.HasProperty(_emissionColorId))
        {
            Debug.LogWarning(
                "[ModeChangePostProcessEffectPlayer] ハンマーのマテリアルに _EmissionColor がありません。",
                this);
            return false;
        }

        _hammerPropertyBlock = new MaterialPropertyBlock();
        Color emissionColor = material.GetColor(_emissionColorId);
        float maxComponent = Mathf.Max(emissionColor.r, emissionColor.g, emissionColor.b);
        float currentIntensity = maxComponent > 1f ? Mathf.Log(maxComponent, 2f) : 0f;

        _hammerEmissionBaseColor = emissionColor / Mathf.Pow(2f, currentIntensity);
        if (_hammerEmissionBaseColor.maxColorComponent <= 0f)
            _hammerEmissionBaseColor = Color.white;

        _currentHammerEmissionColor = emissionColor;
        _isHammerEmissionInitialized = true;
        return true;
    }

    private async UniTaskVoid ChangeHammerEmissionAsync(Color targetColor, CancellationToken token)
    {
        Color startColor = _currentHammerEmissionColor;
        float elapsed = 0f;

        try
        {
            while (elapsed < _changeDuration)
            {
                elapsed += Time.deltaTime;
                ApplyHammerEmissionColor(Color.Lerp(startColor, targetColor,
                    Mathf.Clamp01(elapsed / _changeDuration)));
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            ApplyHammerEmissionColor(targetColor);
        }
        catch (OperationCanceledException)
        {
            // 次のモード変更時は、その時点の色から新しい遷移を開始する。
        }
    }

    private void ApplyHammerEmissionColor(Color color)
    {
        _hammerRenderer.GetPropertyBlock(_hammerPropertyBlock);
        _hammerPropertyBlock.SetColor(_emissionColorId, color);
        _hammerRenderer.SetPropertyBlock(_hammerPropertyBlock);
        _currentHammerEmissionColor = color;
    }

    private void StopEmissionChange()
    {
        _emissionCts?.Cancel();
        _emissionCts?.Dispose();
        _emissionCts = null;
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
