using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// カメラとプレイヤーの間に入ったRendererを、近接エフェクト制御とは独立して一時的に透過します。
/// 元の共有Materialは変更せず、遮蔽中だけ実行時Materialへ差し替えます。
/// </summary>
public sealed class CameraOcclusionTransparencyController
{
    public CameraOcclusionTransparencyController(
        Transform playerTransform,
        Camera mainCamera,
        LayerMask occlusionMask,
        float castRadius,
        float occludedAlpha,
        float fadeSpeed)
    {
        _playerTransform = playerTransform;
        _mainCamera = mainCamera;
        _occlusionMask = occlusionMask;
        _castRadius = Mathf.Max(0f, castRadius);
        _occludedAlpha = Mathf.Clamp01(occludedAlpha);
        _fadeSpeed = Mathf.Max(0.01f, fadeSpeed);
    }

    /// <summary>現在のカメラ位置から遮蔽物を検出し、透過率を更新します。</summary>
    public void UpdateTransparency(float deltaTime)
    {
        if (_playerTransform == null || _mainCamera == null) return;

        _detectedRenderers.Clear();
        DetectOccludingRenderers();

        foreach (Renderer renderer in _detectedRenderers)
        {
            if (!_fadeStates.TryGetValue(renderer, out FadeState state))
            {
                state = CreateFadeState(renderer);
                if (state == null) continue;

                _fadeStates.Add(renderer, state);
            }

            state.IsOccluding = true;
        }

        _removalBuffer.Clear();
        foreach (KeyValuePair<Renderer, FadeState> pair in _fadeStates)
        {
            Renderer renderer = pair.Key;
            FadeState state = pair.Value;
            if (renderer == null)
            {
                DestroyFadeMaterials(state);
                _removalBuffer.Add(renderer);
                continue;
            }

            float targetAlpha = state.IsOccluding ? _occludedAlpha : 1f;
            state.CurrentAlpha = Mathf.MoveTowards(
                state.CurrentAlpha,
                targetAlpha,
                _fadeSpeed * deltaTime);
            ApplyAlpha(state);

            if (!state.IsOccluding && Mathf.Approximately(state.CurrentAlpha, 1f))
            {
                RestoreRenderer(renderer, state);
                _removalBuffer.Add(renderer);
            }

            state.IsOccluding = false;
        }

        foreach (Renderer renderer in _removalBuffer)
        {
            _fadeStates.Remove(renderer);
        }
    }

    /// <summary>シーン切替などでメインカメラが変わった際に参照を更新します。</summary>
    public void SetMainCamera(Camera mainCamera)
    {
        _mainCamera = mainCamera;
    }

    /// <summary>変更したRendererと生成したMaterialをすべて元に戻します。</summary>
    public void Dispose()
    {
        foreach (KeyValuePair<Renderer, FadeState> pair in _fadeStates)
        {
            if (pair.Key != null)
            {
                RestoreRenderer(pair.Key, pair.Value);
            }
            else
            {
                DestroyFadeMaterials(pair.Value);
            }
        }

        _fadeStates.Clear();
        _detectedRenderers.Clear();
        _removalBuffer.Clear();
    }

    private const string SurfaceProperty = "_Surface";
    private const string BlendProperty = "_Blend";
    private const string SrcBlendProperty = "_SrcBlend";
    private const string DstBlendProperty = "_DstBlend";
    private const string ZWriteProperty = "_ZWrite";
    private const string BaseColorProperty = "_BaseColor";
    private const string ColorProperty = "_Color";

    private readonly Transform _playerTransform;
    private Camera _mainCamera;
    private readonly LayerMask _occlusionMask;
    private readonly float _castRadius;
    private readonly float _occludedAlpha;
    private readonly float _fadeSpeed;
    private readonly HashSet<Renderer> _detectedRenderers = new();
    private readonly Dictionary<Renderer, FadeState> _fadeStates = new();
    private readonly List<Renderer> _removalBuffer = new();

    private void DetectOccludingRenderers()
    {
        Vector3 cameraPosition = _mainCamera.transform.position;
        Vector3 direction = _playerTransform.position - cameraPosition;
        float distance = direction.magnitude;
        if (distance <= Mathf.Epsilon) return;

        RaycastHit[] hits = Physics.SphereCastAll(
            cameraPosition,
            _castRadius,
            direction / distance,
            distance,
            _occlusionMask,
            QueryTriggerInteraction.Ignore);

        foreach (RaycastHit hit in hits)
        {
            if (hit.transform == _playerTransform || hit.transform.IsChildOf(_playerTransform)) continue;

            Renderer[] renderers = hit.collider.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                renderers = hit.collider.GetComponentsInParent<Renderer>();
            }

            foreach (Renderer renderer in renderers)
            {
                if (renderer != null && !renderer.transform.IsChildOf(_playerTransform))
                {
                    _detectedRenderers.Add(renderer);
                }
            }
        }
    }

    private FadeState CreateFadeState(Renderer renderer)
    {
        Material[] originalMaterials = renderer.sharedMaterials;
        if (originalMaterials.Length == 0) return null;

        Material[] fadeMaterials = new Material[originalMaterials.Length];
        for (int index = 0; index < originalMaterials.Length; index++)
        {
            Material originalMaterial = originalMaterials[index];
            if (originalMaterial == null) continue;

            Material fadeMaterial = new(originalMaterial)
            {
                name = $"{originalMaterial.name} (Camera Occlusion)",
                renderQueue = (int)RenderQueue.Transparent
            };
            ConfigureTransparentMaterial(fadeMaterial);
            fadeMaterials[index] = fadeMaterial;
        }

        renderer.sharedMaterials = fadeMaterials;
        return new FadeState(originalMaterials, fadeMaterials);
    }

    private static void ConfigureTransparentMaterial(Material material)
    {
        if (material.HasProperty(SurfaceProperty)) material.SetFloat(SurfaceProperty, 1f);
        if (material.HasProperty(BlendProperty)) material.SetFloat(BlendProperty, 0f);
        if (material.HasProperty(SrcBlendProperty)) material.SetFloat(SrcBlendProperty, (float)BlendMode.SrcAlpha);
        if (material.HasProperty(DstBlendProperty)) material.SetFloat(DstBlendProperty, (float)BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty(ZWriteProperty)) material.SetFloat(ZWriteProperty, 0f);

        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.SetOverrideTag("RenderType", "Transparent");
    }

    private static void ApplyAlpha(FadeState state)
    {
        foreach (Material material in state.FadeMaterials)
        {
            if (material == null) continue;

            string colorProperty = material.HasProperty(BaseColorProperty)
                ? BaseColorProperty
                : ColorProperty;
            if (!material.HasProperty(colorProperty)) continue;

            Color color = material.GetColor(colorProperty);
            color.a = state.CurrentAlpha;
            material.SetColor(colorProperty, color);
        }
    }

    private static void RestoreRenderer(Renderer renderer, FadeState state)
    {
        renderer.sharedMaterials = state.OriginalMaterials;
        DestroyFadeMaterials(state);
    }

    private static void DestroyFadeMaterials(FadeState state)
    {
        foreach (Material material in state.FadeMaterials)
        {
            if (material != null)
            {
                Object.Destroy(material);
            }
        }
    }

    private sealed class FadeState
    {
        public Material[] OriginalMaterials { get; }
        public Material[] FadeMaterials { get; }
        public float CurrentAlpha { get; set; } = 1f;
        public bool IsOccluding { get; set; }

        public FadeState(Material[] originalMaterials, Material[] fadeMaterials)
        {
            OriginalMaterials = originalMaterials;
            FadeMaterials = fadeMaterials;
        }
    }
}
