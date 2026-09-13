using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Lighting / Environment の実行時設定。シーン参照はSequenceManager側で指定する。</summary>
[CreateAssetMenu(fileName = "EnvironmentLighting", menuName = "Game/Environment Lighting")]
public sealed class EnvironmentLightingData : ScriptableObject
{
    [Header("Environment")]
    [SerializeField] private Material _skybox = null;
    [SerializeField] private Color _subtractiveShadowColor = new Color(0.42f, 0.48f, 0.63f);
    [Header("Environment Lighting")]
    [SerializeField] private AmbientMode _ambientMode = AmbientMode.Trilight;
    [SerializeField, ColorUsage(false, true)] private Color _ambientSkyColor = Color.gray;
    [SerializeField, ColorUsage(false, true)] private Color _ambientEquatorColor = Color.gray;
    [SerializeField, ColorUsage(false, true)] private Color _ambientGroundColor = Color.gray;
    [SerializeField, ColorUsage(false, true)] private Color _ambientLight = Color.gray;
    [SerializeField, Min(0f)] private float _ambientIntensity = 1f;
    [Header("Environment Reflections")]
    [SerializeField] private DefaultReflectionMode _defaultReflectionMode = DefaultReflectionMode.Skybox;
    [SerializeField] private int _defaultReflectionResolution = 128;
    [SerializeField] private Cubemap _customReflection = null;
    [SerializeField, Min(0f)] private float _reflectionIntensity = 1f;
    [SerializeField, Range(1, 5)] private int _reflectionBounces = 1;
    [Header("Fog")]
    [SerializeField] private bool _fog = false;
    [SerializeField] private Color _fogColor = Color.gray;
    [SerializeField] private FogMode _fogMode = FogMode.ExponentialSquared;
    [SerializeField, Min(0f)] private float _fogDensity = 0.01f;
    [SerializeField, Min(0f)] private float _fogStartDistance = 0f;
    [SerializeField, Min(0f)] private float _fogEndDistance = 300f;
    [Header("Other Settings")]
    [SerializeField, Min(0f)] private float _haloStrength = 0.5f;
    [SerializeField, Min(0f)] private float _flareStrength = 1f;
    [SerializeField, Min(0f)] private float _flareFadeSpeed = 3f;

    [Header("Ambient Probe (Advanced)")]
    [SerializeField, Tooltip("自動生成される環境プローブを27個のSH係数で上書きします")]
    private bool _overrideAmbientProbe;
    [SerializeField] private float[] _ambientProbeCoefficients = new float[27];
    [SerializeField, Tooltip("Skybox変更時に環境更新を要求。反映タイミングは描画環境に依存します")]
    private bool _updateEnvironment = true;

    public void Apply()
    {
        RenderSettings.skybox = _skybox;
        RenderSettings.subtractiveShadowColor = _subtractiveShadowColor;
        RenderSettings.ambientMode = _ambientMode;
        RenderSettings.ambientSkyColor = _ambientSkyColor;
        RenderSettings.ambientEquatorColor = _ambientEquatorColor;
        RenderSettings.ambientGroundColor = _ambientGroundColor;
        // ambientLightはambientSkyColorと同じ内部値を使うため、Flatのときだけ設定する。
        if (_ambientMode == AmbientMode.Flat)
            RenderSettings.ambientLight = _ambientLight;
        RenderSettings.ambientIntensity = _ambientIntensity;
        RenderSettings.defaultReflectionMode = _defaultReflectionMode;
        RenderSettings.defaultReflectionResolution = _defaultReflectionResolution;
        RenderSettings.customReflectionTexture = _customReflection;
        RenderSettings.reflectionIntensity = _reflectionIntensity;
        RenderSettings.reflectionBounces = _reflectionBounces;
        RenderSettings.fog = _fog;
        RenderSettings.fogColor = _fogColor;
        RenderSettings.fogMode = _fogMode;
        RenderSettings.fogDensity = _fogDensity;
        RenderSettings.fogStartDistance = _fogStartDistance;
        RenderSettings.fogEndDistance = _fogEndDistance;
        RenderSettings.haloStrength = _haloStrength;
        RenderSettings.flareStrength = _flareStrength;
        RenderSettings.flareFadeSpeed = _flareFadeSpeed;
        if (_updateEnvironment && !_overrideAmbientProbe)
            DynamicGI.UpdateEnvironment();

        if (_overrideAmbientProbe && _ambientProbeCoefficients != null && _ambientProbeCoefficients.Length == 27)
        {
            var probe = new SphericalHarmonicsL2();
            for (int channel = 0; channel < 3; channel++)
                for (int coefficient = 0; coefficient < 9; coefficient++)
                    probe[channel, coefficient] = _ambientProbeCoefficients[channel * 9 + coefficient];
            RenderSettings.ambientProbe = probe;
        }
    }

#if UNITY_EDITOR
    [ContextMenu("現在のシーンのEnvironment設定をコピー")]
    private void CaptureCurrentScene()
    {
        UnityEditor.Undo.RecordObject(this, "Capture Environment Lighting");
        _skybox = RenderSettings.skybox;
        _subtractiveShadowColor = RenderSettings.subtractiveShadowColor;
        _ambientMode = RenderSettings.ambientMode;
        _ambientSkyColor = RenderSettings.ambientSkyColor;
        _ambientEquatorColor = RenderSettings.ambientEquatorColor;
        _ambientGroundColor = RenderSettings.ambientGroundColor;
        _ambientLight = RenderSettings.ambientLight;
        _ambientIntensity = RenderSettings.ambientIntensity;
        _defaultReflectionMode = RenderSettings.defaultReflectionMode;
        _defaultReflectionResolution = RenderSettings.defaultReflectionResolution;
        _customReflection = RenderSettings.customReflectionTexture as Cubemap;
        _reflectionIntensity = RenderSettings.reflectionIntensity;
        _reflectionBounces = RenderSettings.reflectionBounces;
        _fog = RenderSettings.fog;
        _fogColor = RenderSettings.fogColor;
        _fogMode = RenderSettings.fogMode;
        _fogDensity = RenderSettings.fogDensity;
        _fogStartDistance = RenderSettings.fogStartDistance;
        _fogEndDistance = RenderSettings.fogEndDistance;
        _haloStrength = RenderSettings.haloStrength;
        _flareStrength = RenderSettings.flareStrength;
        _flareFadeSpeed = RenderSettings.flareFadeSpeed;
        _ambientProbeCoefficients = new float[27];
        var probe = RenderSettings.ambientProbe;
        for (int channel = 0; channel < 3; channel++)
            for (int coefficient = 0; coefficient < 9; coefficient++)
                _ambientProbeCoefficients[channel * 9 + coefficient] = probe[channel, coefficient];
        _overrideAmbientProbe = true;
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif

    private void OnValidate()
    {
        _defaultReflectionResolution = Mathf.Clamp(Mathf.ClosestPowerOfTwo(_defaultReflectionResolution), 16, 2048);
        _fogEndDistance = Mathf.Max(_fogStartDistance, _fogEndDistance);
    }
}
