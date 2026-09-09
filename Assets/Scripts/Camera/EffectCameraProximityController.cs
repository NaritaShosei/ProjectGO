using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Inspectorで指定されたエフェクトを、カメラとの距離に応じて一時的に非表示にします。
/// GameObjectのSetActiveではなくRenderer.enabledを切り替えるため、ParticleSystemの再起動を伴いません。
/// このクラスが非表示にした対象だけを再表示し、他システムによる非表示状態は変更しません。
/// 非表示にした後で他システムがenabledを変更した対象は所有を手放し、以後操作しません。
/// </summary>
public sealed class EffectCameraProximityController
{
    private const float ReactivateMargin = 0.25f; // 非表示から復帰するまでの距離の猶予（m）
    private const float DetectionInterval = 0.05f; // 距離判定の実行間隔（秒）

    public EffectCameraProximityController(
        Camera mainCamera,
        Transform[] effectObjects)
    {
        _mainCamera = mainCamera;

        if (effectObjects == null) return;

        foreach (Transform effectObject in effectObjects)
        {
            if (effectObject != null)
            {
                _effectStates.Add(new EffectState(effectObject));
            }
        }
    }

    /// <summary>対象エフェクトとカメラの距離を確認し、表示状態を更新します。</summary>
    public void UpdateEffects(float cameraRadius, float hideStartDistance, float deltaTime)
    {
        if (_mainCamera == null) return;

        // 距離判定は毎フレームではなく一定間隔で行う。
        _detectionTimer -= deltaTime;
        if (_detectionTimer > 0f) return;
        _detectionTimer = DetectionInterval;

        float hideDistance = Mathf.Max(0f, cameraRadius) + Mathf.Max(0f, hideStartDistance);
        float showDistance = hideDistance + ReactivateMargin;
        float hideDistanceSqr = hideDistance * hideDistance;
        float showDistanceSqr = showDistance * showDistance;
        Vector3 cameraPosition = _mainCamera.transform.position;

        foreach (EffectState effectState in _effectStates)
        {
            if (effectState.Target == null) continue;

            // 非表示フェーズ中は、他システムがenabledを戻した対象を管理下から外す。
            if (effectState.IsHiddenByController) effectState.ReconcileExternalChanges();

            // CameraとEffectのTransform.positionはどちらもワールド座標なので、同じ座標系で距離を判定する。
            float distanceSqr = (cameraPosition - effectState.Target.position).sqrMagnitude;
            if (!effectState.IsHiddenByController && effectState.Target.gameObject.activeSelf && distanceSqr <= hideDistanceSqr)
            {
                effectState.HideRenderers();
            }
            else if (effectState.IsHiddenByController && distanceSqr >= showDistanceSqr)
            {
                effectState.ShowRenderers();
            }
        }
    }

    /// <summary>シーン切替などでメインカメラが変わった際に参照を更新します。</summary>
    public void SetMainCamera(Camera mainCamera)
    {
        _mainCamera = mainCamera;
    }

    /// <summary>このクラスが非表示にしたエフェクトを元の表示状態へ戻します。</summary>
    public void Dispose()
    {
        foreach (EffectState effectState in _effectStates)
        {
            if (effectState.Target != null && effectState.IsHiddenByController)
            {
                effectState.ShowRenderers();
            }
        }

        _effectStates.Clear();
    }

    private Camera _mainCamera;
    private float _detectionTimer;
    private readonly List<EffectState> _effectStates = new();

    private sealed class EffectState
    {
        private const string ExcludedObjectName = "Light";

        public Transform Target { get; }
        public bool IsHiddenByController { get; private set; }

        public EffectState(Transform target)
        {
            Target = target;

            // ルート直下のLight以外の子から、配下のRendererをすべて集めて表示切替の対象にする。
            foreach (Transform childTransform in target)
            {
                if (childTransform.name == ExcludedObjectName) continue;

                foreach (Renderer renderer in childTransform.GetComponentsInChildren<Renderer>(true))
                {
                    _renderers.Add(renderer);
                }
            }
        }

        public void HideRenderers()
        {
            foreach (Renderer renderer in _renderers)
            {
                if (renderer == null || !renderer.enabled) continue;

                renderer.enabled = false;
                _claimedRenderers.Add(renderer);
            }

            IsHiddenByController = true;
        }

        public void ShowRenderers()
        {
            foreach (Renderer renderer in _claimedRenderers)
            {
                // 外部がtrueに戻していたら触らず、自分がfalseにした分だけ戻す。
                if (renderer != null && !renderer.enabled) renderer.enabled = true;
            }

            _claimedRenderers.Clear();
            IsHiddenByController = false;
        }

        /// <summary>非表示フェーズ中に他システムがenabledを戻した対象を管理下から外します。</summary>
        public void ReconcileExternalChanges()
        {
            if (_claimedRenderers.Count == 0) return;

            // falseにしたはずがtrueになっている対象は他システムが変更したとみなす。
            _releaseBuffer.Clear();
            foreach (Renderer renderer in _claimedRenderers)
            {
                if (renderer == null || renderer.enabled) _releaseBuffer.Add(renderer);
            }
            if (_releaseBuffer.Count == 0) return;

            foreach (Renderer renderer in _releaseBuffer) _claimedRenderers.Remove(renderer);

            // 所有をすべて手放したら通常の距離判定へ復帰させる。
            if (_claimedRenderers.Count == 0) IsHiddenByController = false;
        }

        private readonly List<Renderer> _renderers = new();
        private readonly HashSet<Renderer> _claimedRenderers = new();
        private readonly List<Renderer> _releaseBuffer = new();
    }
}
