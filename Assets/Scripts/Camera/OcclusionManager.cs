using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// カメラ-プレイヤー間のオブジェクト遮蔽を管理します。
/// Raycastで検知したオブジェクトをカメラとの距離に応じて段階的に透過させます。
/// </summary>
public class OcclusionManager : MonoBehaviour
{
    #region Inspector フィールド

    [Header("遮蔽判定")]
    [Tooltip("Raycastレイヤーマスク（遮蔽対象を指定）")]
    [SerializeField] private LayerMask _occlusionLayerMask = -1;
    [Tooltip("Raycast判定から除外するタグ")]
    [SerializeField] private string[] _ignoreTags = { "Player", "Enemy" };

    [Header("透明度制御")]
    [Tooltip("カメラからこの距離（m）以下だと最小透明度")]
    [SerializeField] private float _minDistanceThreshold = 1f;
    [Tooltip("カメラからこの距離（m）以上だと最大透明度（不透明）")]
    [SerializeField] private float _maxDistanceThreshold = 5f;
    [Tooltip("最小透明度（0=完全透明、1=不透明）")]
    [SerializeField] private float _minAlpha = 0.2f;
    [Tooltip("最大透明度（0=完全透明、1=不透明）")]
    [SerializeField] private float _maxAlpha = 1f;

    [Header("フェード設定")]
    [Tooltip("透明度変更速度（0で瞬時、大きいほどゆっくり）")]
    [SerializeField] private float _fadeSpeed = 3f;

    [Header("マテリアル")]
    [Tooltip("透明化用のトランスペアレントマテリアルテンプレート（Shaderが透明対応のもの）")]
    [SerializeField] private Material _transparentMaterialTemplate;

    #endregion

    #region プライベートフィールド

    private Camera _mainCamera;
    private Transform _playerTransform;
    private Transform _cameraTransform;

    /// <summary>遮蔽中のオブジェクト情報：Renderer → (オリジナルマテリアル配列, 現在の目標Alpha)</summary>
    private Dictionary<Renderer, OcclusionData> _occludingObjects = new();

    private struct OcclusionData
    {
        public Material[] originalMaterials;
        public Material[] currentMaterials;
        public float targetAlpha;
    }

    #endregion

    #region Unityライフサイクル

    private void Awake()
    {
        _mainCamera = Camera.main;
        if (_mainCamera == null)
        {
            Debug.LogError("[OcclusionManager] MainCamera not found.");
            enabled = false;
            return;
        }

        _cameraTransform = _mainCamera.transform;
    }

    private void Start()
    {
        if (ServiceLocator.TryGet(out CameraManager cameraManager))
        {
            // CameraManagerから参照を取得（必要に応じて）
        }
    }

    private void LateUpdate()
    {
        if (_playerTransform == null) return;

        UpdateOcclusion();
    }

    private void OnDestroy()
    {
        RestoreAllMaterials();
    }

    #endregion

    #region 初期化

    /// <summary>
    /// プレイヤー参照を設定します。
    /// </summary>
    public void Init(Transform playerTransform)
    {
        _playerTransform = playerTransform;
    }

    #endregion

    #region 遮蔽処理

    /// <summary>
    /// カメラ-プレイヤー間のオブジェクトを検知し、距離ベースで透明度を制御します。
    /// </summary>
    private void UpdateOcclusion()
    {
        Debug.Log($"カリング中{_occludingObjects.Count}件");
        Vector3 cameraPos = _cameraTransform.position;
        Vector3 playerPos = _playerTransform.position;
        Vector3 rayDir = (playerPos - cameraPos).normalized;
        float rayDistance = Vector3.Distance(cameraPos, playerPos);

        // Raycastで遮蔽オブジェクトを全検出
        var hits = Physics.RaycastAll(cameraPos, rayDir, rayDistance, _occlusionLayerMask)
            .OrderBy(h => h.distance)
            .ToList();

        var detectedRenderers = new HashSet<Renderer>();

        foreach (var hit in hits)
        {
            if (ShouldIgnore(hit.collider.gameObject)) continue;

            var renderer = hit.collider.GetComponent<Renderer>();
            if (renderer == null) continue;

            detectedRenderers.Add(renderer);

            // 初回検知 → オリジナルマテリアルを保存
            if (!_occludingObjects.ContainsKey(renderer))
            {
                RegisterOccludingObject(renderer);
            }

            // 距離に応じた目標Alphaを計算
            float targetAlpha = CalculateTargetAlpha(hit.distance);

            _occludingObjects[renderer] = new OcclusionData
            {
                originalMaterials = _occludingObjects[renderer].originalMaterials,
                currentMaterials = _occludingObjects[renderer].currentMaterials,
                targetAlpha = targetAlpha
            };
        }

        // 現在のフレームで検知されなかったオブジェクトを復帰開始
        var noLongerOccluding = _occludingObjects.Keys
            .Where(r => !detectedRenderers.Contains(r))
            .ToList();

        foreach (var renderer in noLongerOccluding)
        {
            _occludingObjects[renderer] = new OcclusionData
            {
                originalMaterials = _occludingObjects[renderer].originalMaterials,
                currentMaterials = _occludingObjects[renderer].currentMaterials,
                targetAlpha = _maxAlpha
            };
        }

        // 全遮蔽オブジェクトのAlphaをフェード更新
        UpdateAllAlphas();

        // 完全に復帰したオブジェクトを削除
        RemoveFullyRestoredObjects();
    }

    /// <summary>
    /// Raycast判定の距離に基づいて、目標透明度を計算します。
    /// </summary>
    private float CalculateTargetAlpha(float distance)
    {
        // 距離 ≤ minThreshold → 最小透明度
        // 距離 ≥ maxThreshold → 最大透明度（不透明）
        // その間を線形補間

        if (distance <= _minDistanceThreshold)
            return _minAlpha;

        if (distance >= _maxDistanceThreshold)
            return _maxAlpha;

        float t = (distance - _minDistanceThreshold) / (_maxDistanceThreshold - _minDistanceThreshold);
        return Mathf.Lerp(_minAlpha, _maxAlpha, t);
    }

    /// <summary>
    /// オブジェクトを遮蔽リストに登録し、マテリアルを透明化対応にセットアップします。
    /// </summary>
    private void RegisterOccludingObject(Renderer renderer)
    {
        Material[] originalMaterials = renderer.materials;
        Material[] transparentMaterials = new Material[originalMaterials.Length];

        // 各マテリアルをトランスペアレント版に置き換え
        for (int i = 0; i < originalMaterials.Length; i++)
        {
            if (_transparentMaterialTemplate != null)
            {
                transparentMaterials[i] = new Material(_transparentMaterialTemplate);
                // オリジナルマテリアルのテクスチャなどを引き継ぐ（必要に応じて）
                CopyTextureProperties(originalMaterials[i], transparentMaterials[i]);
            }
            else
            {
                // テンプレートがない場合、オリジナルマテリアルをコピーして使用
                transparentMaterials[i] = new Material(originalMaterials[i]);
            }
        }

        renderer.materials = transparentMaterials;

        _occludingObjects[renderer] = new OcclusionData
        {
            originalMaterials = originalMaterials,
            currentMaterials = transparentMaterials,
            targetAlpha = _maxAlpha
        };

        Debug.Log($"[OcclusionManager] Registered: {renderer.gameObject.name}");
    }

    /// <summary>
    /// オリジナルマテリアルのテクスチャプロパティをコピーします。
    /// </summary>
    private void CopyTextureProperties(Material source, Material dest)
    {
        // よくあるプロパティ名を対象にコピー
        string[] textureProps = { "_MainTex", "_BaseMap", "_BumpMap", "_NormalMap" };

        foreach (var propName in textureProps)
        {
            if (source.HasProperty(propName))
            {
                var texture = source.GetTexture(propName);
                if (texture != null)
                    dest.SetTexture(propName, texture);
            }
        }

        // ベースカラーもコピー
        if (source.HasProperty("_BaseColor"))
            dest.SetColor("_BaseColor", source.GetColor("_BaseColor"));
        else if (source.HasProperty("_Color"))
            dest.SetColor("_Color", source.GetColor("_Color"));
    }

    /// <summary>
    /// 全遮蔽オブジェクトのAlpha値をフェード更新します。
    /// </summary>
    private void UpdateAllAlphas()
    {
        foreach (var renderer in _occludingObjects.Keys.ToList())
        {
            var data = _occludingObjects[renderer];

            // 各マテリアルのAlphaを目標値に向けてLerp
            foreach (var material in data.currentMaterials)
            {
                if (!material.HasProperty("_Alpha"))
                    continue;

                float currentAlpha = material.GetFloat("_Alpha");
                float newAlpha = Mathf.Lerp(currentAlpha, data.targetAlpha, Time.deltaTime * _fadeSpeed);
                material.SetFloat("_Alpha", newAlpha);
            }

            data.currentMaterials = renderer.materials; // 更新を反映
            _occludingObjects[renderer] = data;
        }
    }

    /// <summary>
    /// 完全に復帰したオブジェクト（Alpha=1.0に到達）をリストから削除し、オリジナルマテリアルを復帰させます。
    /// </summary>
    private void RemoveFullyRestoredObjects()
    {
        var fullyRestored = _occludingObjects
            .Where(kvp => Mathf.Approximately(kvp.Value.targetAlpha, _maxAlpha) &&
                         kvp.Value.currentMaterials.All(m => Mathf.Approximately(m.GetFloat("_Alpha"), _maxAlpha)))
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var renderer in fullyRestored)
        {
            renderer.materials = _occludingObjects[renderer].originalMaterials;
            _occludingObjects.Remove(renderer);
            Debug.Log($"[OcclusionManager] Removed: {renderer.gameObject.name}");
        }
    }

    /// <summary>
    /// 無視対象のオブジェクトかチェックします。
    /// </summary>
    private bool ShouldIgnore(GameObject obj)
    {
        return _ignoreTags.Any(tag => obj.CompareTag(tag));
    }

    /// <summary>
    /// 全遮蔽オブジェクトのマテリアルをオリジナルに復帰させます（破棄時）。
    /// </summary>
    private void RestoreAllMaterials()
    {
        foreach (var kvp in _occludingObjects)
        {
            kvp.Key.materials = kvp.Value.originalMaterials;
        }

        _occludingObjects.Clear();
    }

    #endregion

    #region デバッグ

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || _playerTransform == null || _cameraTransform == null)
            return;

        // Ray可視化
        Vector3 cameraPos = _cameraTransform.position;
        Vector3 playerPos = _playerTransform.position;
        Vector3 rayDir = (playerPos - cameraPos).normalized;
        float rayDistance = Vector3.Distance(cameraPos, playerPos);

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(cameraPos, rayDir * rayDistance);

        // 距離閾値の可視化
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(cameraPos, _minDistanceThreshold);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(cameraPos, _maxDistanceThreshold);
    }
#endif

    #endregion
}