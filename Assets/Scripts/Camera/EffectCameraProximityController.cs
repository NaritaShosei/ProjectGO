using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Inspectorで指定されたエフェクトを、カメラとの距離に応じて一時的に非表示にします。
/// このクラスが非表示にした対象だけを再表示し、他システムによる非表示状態は変更しません。
/// </summary>
public sealed class EffectCameraProximityController
{
    private const float ReactivateMargin = 0.25f;

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
    public void UpdateEffects(float cameraRadius, float hideStartDistance)
    {
        if (_mainCamera == null) return;

        float hideDistance = Mathf.Max(0f, cameraRadius) + Mathf.Max(0f, hideStartDistance);
        float showDistance = hideDistance + ReactivateMargin;
        Vector3 cameraPosition = _mainCamera.transform.position;

        foreach (EffectState effectState in _effectStates)
        {
            if (effectState.Target == null) continue;

            // CameraとEffectのTransform.positionはどちらもワールド座標なので、同じ座標系で距離を判定する。
            float distance = Vector3.Distance(cameraPosition, effectState.Target.position);
            if (!effectState.IsHiddenByController && effectState.Target.gameObject.activeSelf && distance <= hideDistance)
            {
                effectState.HideObjects();
            }
            else if (effectState.IsHiddenByController && distance >= showDistance)
            {
                effectState.ShowObjects();
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
                effectState.ShowObjects();
            }
        }

        _effectStates.Clear();
    }

    private Camera _mainCamera;
    private readonly List<EffectState> _effectStates = new();

    private sealed class EffectState
    {
        private const string ExcludedObjectName = "Light";

        public Transform Target { get; }
        public bool IsHiddenByController { get; private set; }

        public EffectState(Transform target)
        {
            Target = target;

            // Effectのルートは残し、直下にあるLight以外だけを表示切替の対象にする。
            foreach (Transform childTransform in target)
            {
                if (childTransform.name == ExcludedObjectName) continue;
                _childStates.Add(new ChildState(childTransform.gameObject));
            }
        }

        public void HideObjects()
        {
            foreach (ChildState childState in _childStates)
            {
                if (childState.Target == null || !childState.Target.activeSelf) continue;

                childState.Target.SetActive(false);
                childState.IsHiddenByController = true;
            }

            IsHiddenByController = true;
        }

        public void ShowObjects()
        {
            foreach (ChildState childState in _childStates)
            {
                if (childState.Target == null || !childState.IsHiddenByController) continue;

                childState.Target.SetActive(true);
                childState.IsHiddenByController = false;
            }

            IsHiddenByController = false;
        }

        private readonly List<ChildState> _childStates = new();
    }

    private sealed class ChildState
    {
        public GameObject Target { get; }
        public bool IsHiddenByController { get; set; }

        public ChildState(GameObject target)
        {
            Target = target;
        }
    }
}
