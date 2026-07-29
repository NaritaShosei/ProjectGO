#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class AttackEffectLayoutWindow : EditorWindow
{
    [MenuItem("Tools/Attack/Effect Layout Editor")]
    private static void OpenFromMenu() => Open(null);

    public static void Open(AttackData data, int variantIndex = 0)
    {
        var window = GetWindow<AttackEffectLayoutWindow>("Attack Effect Layout");
        window.minSize = new Vector2(390f, 430f);
        window.SetData(data, variantIndex);
        window.Show();
    }

    private AttackData _data;
    private SerializedObject _serializedData;
    private GameObject _referenceObject;
    private GameObject _previewInstance;
    private int _variantIndex;
    private int _hitIndex;
    private TransformTool _tool;
    private Vector2 _scroll;
    private bool _isPreviewPlaying;
    private double _lastEditorTime;

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        EditorApplication.update += UpdatePreviewPlayback;
        _lastEditorTime = EditorApplication.timeSinceStartup;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        EditorApplication.update -= UpdatePreviewPlayback;
        DestroyPreview();
    }

    private void SetData(AttackData data, int variantIndex)
    {
        _data = data;
        _serializedData = data != null ? new SerializedObject(data) : null;
        _variantIndex = variantIndex;
        _hitIndex = 0;
        Repaint();
    }

    private void OnGUI()
    {
        EditorGUI.BeginChangeCheck();
        var selected = (AttackData)EditorGUILayout.ObjectField("Attack Data", _data, typeof(AttackData), false);
        if (EditorGUI.EndChangeCheck())
            SetData(selected, 0);

        if (_data == null || _serializedData == null)
        {
            EditorGUILayout.HelpBox("編集するAttackDataを選択してください。", MessageType.Info);
            return;
        }

        _serializedData.Update();
        var variants = _serializedData.FindProperty("_variants");
        if (variants == null || variants.arraySize == 0)
        {
            EditorGUILayout.HelpBox("Variantがありません。", MessageType.Warning);
            return;
        }

        _variantIndex = Mathf.Clamp(_variantIndex, 0, variants.arraySize - 1);
        _variantIndex = EditorGUILayout.Popup("Charge / Variant", _variantIndex, BuildVariantNames(variants));
        var variant = variants.GetArrayElementAtIndex(_variantIndex);
        var hits = variant.FindPropertyRelative("_hits");
        if (hits == null || hits.arraySize == 0)
        {
            EditorGUILayout.HelpBox("Hitデータがありません。", MessageType.Warning);
            return;
        }

        _hitIndex = Mathf.Clamp(_hitIndex, 0, hits.arraySize - 1);
        if (hits.arraySize > 1)
            _hitIndex = EditorGUILayout.IntSlider("Hit", _hitIndex, 0, hits.arraySize - 1);

        var hit = hits.GetArrayElementAtIndex(_hitIndex);
        var effect = hit.FindPropertyRelative("AttackEffect");
        if (effect == null)
        {
            EditorGUILayout.HelpBox("AttackEffect設定がありません。", MessageType.Error);
            return;
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Preview Objects", EditorStyles.boldLabel);
        _referenceObject = (GameObject)EditorGUILayout.ObjectField(
            "Reference Object", _referenceObject, typeof(GameObject), true);
        EditorGUILayout.PropertyField(effect.FindPropertyRelative("_previewPrefab"), new GUIContent("Target VFX"));

        EditorGUILayout.BeginHorizontal();
        if (_previewInstance == null)
        {
            if (GUILayout.Button("Show Preview", GUILayout.Height(24))) CreatePreview(effect);
        }
        else
        {
            if (GUILayout.Button("Refresh Preview", GUILayout.Height(24))) CreatePreview(effect);
            if (GUILayout.Button("Hide Preview", GUILayout.Height(24))) DestroyPreview();
        }
        EditorGUILayout.EndHorizontal();

        using (new EditorGUI.DisabledScope(_previewInstance == null))
        {
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("Playback", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("▶ Play", GUILayout.Height(25))) PlayPreview();
            if (GUILayout.Button("Ⅱ Pause", GUILayout.Height(25))) PausePreview();
            if (GUILayout.Button("↻ Restart", GUILayout.Height(25))) RestartPreview();
            if (GUILayout.Button("■ Stop", GUILayout.Height(25))) StopPreview();
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(7);
        EditorGUILayout.LabelField("Transform", EditorStyles.boldLabel);
        _tool = (TransformTool)GUILayout.Toolbar((int)_tool, new[] { "Move", "Rotate", "Scale" });

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        EditorGUILayout.PropertyField(effect.FindPropertyRelative("_localPosition"), new GUIContent("Local Position"));
        EditorGUILayout.PropertyField(effect.FindPropertyRelative("_localEulerAngles"), new GUIContent("Local Rotation"));
        EditorGUILayout.PropertyField(effect.FindPropertyRelative("_localScale"), new GUIContent("Additional Scale"));

        EditorGUILayout.Space(7);
        EditorGUILayout.LabelField("Automatic Size", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(effect.FindPropertyRelative("_fitToAttackArea"), new GUIContent("Fit To Attack Area"));
        EditorGUILayout.PropertyField(effect.FindPropertyRelative("_baseSize"), new GUIContent("VFX Base Size"));

        float range = hit.FindPropertyRelative("AttackRange").floatValue;
        float radius = hit.FindPropertyRelative("AttackRadius").floatValue;
        Vector3 finalScale = CalculateScale(hit, effect);
        EditorGUILayout.HelpBox(
            $"Attack Range: {range:F2}  Radius: {radius:F2}\nFinal Scale: {finalScale.x:F3}, {finalScale.y:F3}, {finalScale.z:F3}",
            MessageType.None);

        EditorGUILayout.Space(7);
        EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(effect.FindPropertyRelative("_enabled"));
        EditorGUILayout.PropertyField(effect.FindPropertyRelative("_effectKey"));
        EditorGUILayout.EndScrollView();

        if (_serializedData.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(_data);
            SceneView.RepaintAll();
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (_serializedData == null || _previewInstance == null) return;
        if (!TryGetHitAndEffect(out var hit, out var effect)) return;

        GetReferenceTransform(sceneView, out Vector3 pivot, out Quaternion referenceRotation);
        Vector3 localPosition = effect.FindPropertyRelative("_localPosition").vector3Value;
        Vector3 localEuler = effect.FindPropertyRelative("_localEulerAngles").vector3Value;
        Vector3 worldPosition = pivot + referenceRotation * localPosition;
        Quaternion worldRotation = referenceRotation * Quaternion.Euler(localEuler);

        _previewInstance.transform.SetPositionAndRotation(worldPosition, worldRotation);
        _previewInstance.transform.localScale = CalculateScale(hit, effect);
        DrawAttackArea(pivot, referenceRotation * Vector3.forward, hit);

        EditorGUI.BeginChangeCheck();
        switch (_tool)
        {
            case TransformTool.Move:
                worldPosition = Handles.PositionHandle(worldPosition, worldRotation);
                break;
            case TransformTool.Rotate:
                worldRotation = Handles.RotationHandle(worldRotation, worldPosition);
                break;
            case TransformTool.Scale:
                var scaleProperty = effect.FindPropertyRelative("_localScale");
                scaleProperty.vector3Value = Handles.ScaleHandle(
                    scaleProperty.vector3Value, worldPosition, worldRotation,
                    HandleUtility.GetHandleSize(worldPosition));
                break;
        }

        if (!EditorGUI.EndChangeCheck()) return;
        Undo.RecordObject(_data, "Edit Attack Effect Layout");
        if (_tool == TransformTool.Move)
            effect.FindPropertyRelative("_localPosition").vector3Value =
                Quaternion.Inverse(referenceRotation) * (worldPosition - pivot);
        else if (_tool == TransformTool.Rotate)
            effect.FindPropertyRelative("_localEulerAngles").vector3Value =
                (Quaternion.Inverse(referenceRotation) * worldRotation).eulerAngles;

        _serializedData.ApplyModifiedProperties();
        EditorUtility.SetDirty(_data);
        Repaint();
    }

    private bool TryGetHitAndEffect(out SerializedProperty hit, out SerializedProperty effect)
    {
        _serializedData.Update();
        var variants = _serializedData.FindProperty("_variants");
        if (variants == null || variants.arraySize == 0)
        {
            hit = effect = null;
            return false;
        }
        _variantIndex = Mathf.Clamp(_variantIndex, 0, variants.arraySize - 1);
        var hits = variants.GetArrayElementAtIndex(_variantIndex).FindPropertyRelative("_hits");
        if (hits == null || hits.arraySize == 0)
        {
            hit = effect = null;
            return false;
        }
        _hitIndex = Mathf.Clamp(_hitIndex, 0, hits.arraySize - 1);
        hit = hits.GetArrayElementAtIndex(_hitIndex);
        effect = hit.FindPropertyRelative("AttackEffect");
        return effect != null;
    }

    private void GetReferenceTransform(SceneView sceneView, out Vector3 pivot, out Quaternion rotation)
    {
        if (_referenceObject != null)
        {
            pivot = _referenceObject.transform.position;
            rotation = _referenceObject.transform.rotation;
            return;
        }
        pivot = sceneView.camera.transform.position + sceneView.camera.transform.forward * 5f;
        pivot.y = 0f;
        rotation = Quaternion.identity;
    }

    private void CreatePreview(SerializedProperty effect)
    {
        DestroyPreview();
        var prefab = effect.FindPropertyRelative("_previewPrefab").objectReferenceValue as GameObject;
        if (prefab == null) return;
        _previewInstance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (_previewInstance == null) _previewInstance = Instantiate(prefab);
        _previewInstance.hideFlags = HideFlags.HideAndDontSave;
        RestartPreview();
        SceneView.RepaintAll();
    }

    private void DestroyPreview()
    {
        if (_previewInstance == null) return;
        DestroyImmediate(_previewInstance);
        _previewInstance = null;
        _isPreviewPlaying = false;
        SceneView.RepaintAll();
    }

    private void PlayPreview()
    {
        if (_previewInstance == null) return;
        foreach (var particle in GetRootParticles())
            particle.Play(true);
        _isPreviewPlaying = true;
        _lastEditorTime = EditorApplication.timeSinceStartup;
    }

    private void PausePreview()
    {
        if (_previewInstance == null) return;
        foreach (var particle in GetRootParticles())
            particle.Pause(true);
        _isPreviewPlaying = false;
        SceneView.RepaintAll();
    }

    private void RestartPreview()
    {
        if (_previewInstance == null) return;
        foreach (var particle in GetRootParticles())
        {
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particle.Simulate(0f, true, true, true);
            particle.Play(true);
        }
        _isPreviewPlaying = true;
        _lastEditorTime = EditorApplication.timeSinceStartup;
        SceneView.RepaintAll();
    }

    private void StopPreview()
    {
        if (_previewInstance == null) return;
        foreach (var particle in GetRootParticles())
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _isPreviewPlaying = false;
        SceneView.RepaintAll();
    }

    private ParticleSystem[] GetRootParticles()
    {
        var particles = _previewInstance.GetComponentsInChildren<ParticleSystem>(true);
        var roots = new System.Collections.Generic.List<ParticleSystem>();
        foreach (var particle in particles)
        {
            Transform parent = particle.transform.parent;
            if (parent == null || parent.GetComponentInParent<ParticleSystem>() == null)
                roots.Add(particle);
        }
        return roots.ToArray();
    }

    private void UpdatePreviewPlayback()
    {
        double now = EditorApplication.timeSinceStartup;
        float deltaTime = Mathf.Min((float)(now - _lastEditorTime), 0.05f);
        _lastEditorTime = now;
        if (!_isPreviewPlaying || _previewInstance == null || deltaTime <= 0f) return;

        foreach (var particle in GetRootParticles())
            particle.Simulate(deltaTime, true, false, true);

        SceneView.RepaintAll();
        Repaint();
    }

    private static Vector3 CalculateScale(SerializedProperty hit, SerializedProperty effect)
    {
        Vector3 scale = effect.FindPropertyRelative("_localScale").vector3Value;
        if (!effect.FindPropertyRelative("_fitToAttackArea").boolValue) return scale;
        float range = hit.FindPropertyRelative("AttackRange").floatValue;
        float radius = hit.FindPropertyRelative("AttackRadius").floatValue;
        Vector3 size = effect.FindPropertyRelative("_baseSize").vector3Value;
        return Vector3.Scale(new Vector3(
            (range + radius) / Mathf.Max(0.01f, size.x),
            (radius * 2f) / Mathf.Max(0.01f, size.y),
            (radius * 2f) / Mathf.Max(0.01f, size.z)), scale);
    }

    private static void DrawAttackArea(Vector3 pivot, Vector3 forward, SerializedProperty hit)
    {
        float range = hit.FindPropertyRelative("AttackRange").floatValue;
        float radius = hit.FindPropertyRelative("AttackRadius").floatValue;
        Vector3 center = pivot + forward * range;
        Handles.color = new Color(1f, 0.25f, 0.2f, 0.85f);
        Handles.DrawWireDisc(center, Vector3.up, radius);
        Handles.DrawWireDisc(center, Vector3.right, radius);
        Handles.DrawWireDisc(center, Vector3.forward, radius);
        Handles.DrawDottedLine(pivot, center, 4f);
    }

    private static string[] BuildVariantNames(SerializedProperty variants)
    {
        var names = new string[variants.arraySize];
        for (int i = 0; i < variants.arraySize; i++)
        {
            var variant = variants.GetArrayElementAtIndex(i);
            string name = variant.FindPropertyRelative("_attackName").stringValue;
            var charge = (ChargeLevel)variant.FindPropertyRelative("_requiredCharge").intValue;
            names[i] = $"[{i}] {name} ({charge})";
        }
        return names;
    }

    private enum TransformTool { Move, Rotate, Scale }
}
#endif
