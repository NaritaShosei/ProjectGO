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

        // 遮蔽判定は間引く。判定しないフレームは前回の検出結果を使い回す
        if (ShouldDetectThisFrame(deltaTime))
        {
            _detectedRenderers.Clear();
            DetectOccludingRenderers();
        }

        // 新規検出RendererぶんのFadeStateを用意
        foreach (Renderer renderer in _detectedRenderers)
        {
            if (_fadeStates.ContainsKey(renderer)) continue;

            FadeState state = CreateFadeState(renderer);
            if (state == null) continue;

            _fadeStates.Add(renderer, state);
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

            // 今フレームの遮蔽状態は検出セットの中身で判断
            bool isOccluding = _detectedRenderers.Contains(renderer);
            float targetAlpha = isOccluding ? _occludedAlpha : 1f;

            float previousAlpha = state.CurrentAlpha;
            state.CurrentAlpha = Mathf.MoveTowards(
                previousAlpha,
                targetAlpha,
                _fadeSpeed * deltaTime);

            // アルファが動いた時だけMaterialへ反映
            if (!Mathf.Approximately(previousAlpha, state.CurrentAlpha))
            {
                ApplyAlpha(state);
            }

            if (!isOccluding && Mathf.Approximately(state.CurrentAlpha, 1f))
            {
                RestoreRenderer(renderer, state);
                _removalBuffer.Add(renderer);
            }
        }

        foreach (Renderer renderer in _removalBuffer)
        {
            _fadeStates.Remove(renderer);
        }
    }

    /// <summary>間引き間隔かカメラ・プレイヤーの移動量から、今フレーム遮蔽判定するか判断します。</summary>
    private bool ShouldDetectThisFrame(float deltaTime)
    {
        _detectionTimer -= deltaTime;

        Vector3 cameraPosition = _mainCamera.transform.position;
        Vector3 playerPosition = _playerTransform.position;
        float thresholdSqr = DetectionMoveThreshold * DetectionMoveThreshold;

        // 高速なカメラ振り・移動で壁抜けしないよう、大きく動いたら間隔を待たず判定
        bool movedFar =
            (cameraPosition - _lastDetectionCameraPosition).sqrMagnitude > thresholdSqr ||
            (playerPosition - _lastDetectionPlayerPosition).sqrMagnitude > thresholdSqr;

        if (_detectionTimer > 0f && !movedFar) return false;

        _detectionTimer = DetectionInterval;
        _lastDetectionCameraPosition = cameraPosition;
        _lastDetectionPlayerPosition = playerPosition;
        return true;
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
        _colliderRenderers.Clear();
    }

    private const string SurfaceProperty = "_Surface";
    private const string BlendProperty = "_Blend";
    private const string SrcBlendProperty = "_SrcBlend";
    private const string DstBlendProperty = "_DstBlend";
    private const string ZWriteProperty = "_ZWrite";
    private const string BaseColorProperty = "_BaseColor";
    private const string ColorProperty = "_Color";
    private const float DetectionInterval = 0.05f; // 遮蔽判定の実行間隔（秒）
    private const float DetectionMoveThreshold = 0.5f; // 前回判定位置からこの距離以上動いたら間隔を待たず再判定（m）
    private const int MaxHitCount = 64; // SphereCast結果を受けるバッファのサイズ

    private readonly Transform _playerTransform;
    private Camera _mainCamera;
    private readonly LayerMask _occlusionMask;
    private readonly float _castRadius;
    private readonly float _occludedAlpha;
    private readonly float _fadeSpeed;
    private readonly HashSet<Renderer> _detectedRenderers = new();
    private readonly Dictionary<Renderer, FadeState> _fadeStates = new();
    private readonly List<Renderer> _removalBuffer = new();
    private readonly RaycastHit[] _hitBuffer = new RaycastHit[MaxHitCount];
    private readonly Dictionary<Collider, Renderer[]> _colliderRenderers = new();
    private float _detectionTimer;
    private Vector3 _lastDetectionCameraPosition;
    private Vector3 _lastDetectionPlayerPosition;

    private void DetectOccludingRenderers()
    {
        Vector3 cameraPosition = _mainCamera.transform.position;
        Vector3 direction = _playerTransform.position - cameraPosition;
        float distance = direction.magnitude;
        if (distance <= Mathf.Epsilon) return;

        // 結果は再利用バッファへ受ける（毎フレームの配列確保を避ける）
        int hitCount = Physics.SphereCastNonAlloc(
            cameraPosition,
            _castRadius,
            direction / distance,
            _hitBuffer,
            distance,
            _occlusionMask,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            Collider collider = _hitBuffer[i].collider;
            if (collider == null) continue;

            Transform hitTransform = _hitBuffer[i].transform;
            if (hitTransform == null) continue;
            if (hitTransform == _playerTransform || hitTransform.IsChildOf(_playerTransform)) continue;

            // Collider→Renderer解決はキャッシュして階層探索を1回に抑える
            if (!_colliderRenderers.TryGetValue(collider, out Renderer[] renderers))
            {
                renderers = collider.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0)
                {
                    renderers = collider.GetComponentsInParent<Renderer>();
                }

                _colliderRenderers.Add(collider, renderers);
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

        public FadeState(Material[] originalMaterials, Material[] fadeMaterials)
        {
            OriginalMaterials = originalMaterials;
            FadeMaterials = fadeMaterials;
        }
    }
}
