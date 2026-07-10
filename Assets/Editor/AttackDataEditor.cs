#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AttackData))]
[CanEditMultipleObjects]
public class AttackDataEditor : Editor
{
    private SerializedProperty _attackId;
    private SerializedProperty _mode;
    private SerializedProperty _nextComboAttackId;
    private SerializedProperty _insertAfterAttackId;
    private SerializedProperty _isUnlockedBySkill;
    private SerializedProperty _requiredSkillId;
    private SerializedProperty _variants;

    private readonly List<bool> _variantFoldouts = new();
    private int _selectedVariantIndex;
    private bool _showPreview = true;
    private GameObject _previewTarget;
    private bool _isAutoTarget = true;
    private Vector3 _previewDirection = Vector3.forward;
    private float _previewAngle;

    private static readonly Color AttackSphereColor = new(1f, 0.3f, 0.3f, 0.25f);
    private static readonly Color AttackSphereOutline = new(1f, 0.2f, 0.2f, 0.9f);
    private static readonly Color HomingColor = new(0.3f, 0.8f, 1f, 0.15f);
    private static readonly Color HomingOutline = new(0.3f, 0.8f, 1f, 0.8f);
    private static readonly Color MoveColor = new(0.3f, 1f, 0.5f, 0.9f);
    private static readonly Color KnockbackColor = new(1f, 0.85f, 0.1f, 0.9f);

    private void OnEnable()
    {
        _attackId = serializedObject.FindProperty("_attackId");
        _mode = serializedObject.FindProperty("_mode");
        _nextComboAttackId = serializedObject.FindProperty("_nextComboAttackId");
        _insertAfterAttackId = serializedObject.FindProperty("_insertAfterAttackId");
        _isUnlockedBySkill = serializedObject.FindProperty("_isUnlockedBySkill");
        _requiredSkillId = serializedObject.FindProperty("_requiredSkillId");
        _variants = serializedObject.FindProperty("_variants");

        SyncFoldouts();
        TryAutoFindTarget();
        SceneView.duringSceneGui += HandleSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= HandleSceneGUI;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPreviewControls();
        EditorGUILayout.Space(4);

        EditorGUILayout.LabelField("Basic", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_attackId);
        EditorGUILayout.PropertyField(_mode);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Combo", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_nextComboAttackId);
        EditorGUILayout.PropertyField(_insertAfterAttackId);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Skill Unlock", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_isUnlockedBySkill);
        if (_isUnlockedBySkill.boolValue)
            EditorGUILayout.PropertyField(_requiredSkillId);

        EditorGUILayout.Space(4);
        DrawVariantsList();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawVariantsList()
    {
        SyncFoldouts();

        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        EditorGUILayout.LabelField("Variants", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Add", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            serializedObject.ApplyModifiedProperties();

            var data = (AttackData)target;
            Undo.RecordObject(data, "Add Attack Variant");
            var variant = new AttackVariantData();
            variant.SetDefaults();
            data.AddVariant(variant);
            EditorUtility.SetDirty(data);

            serializedObject.Update();
            SyncFoldouts();
            _selectedVariantIndex = _variants.arraySize - 1;
        }
        EditorGUILayout.EndHorizontal();

        int deleteIndex = -1;
        for (int i = 0; i < _variants.arraySize; i++)
        {
            var variant = _variants.GetArrayElementAtIndex(i);
            var name = variant.FindPropertyRelative("_attackName").stringValue;
            var charge = variant.FindPropertyRelative("_requiredCharge").intValue;
            var title = string.IsNullOrEmpty(name) ? $"Variant {i}" : name;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            bool selected = GUILayout.Toggle(_selectedVariantIndex == i, GUIContent.none, EditorStyles.radioButton, GUILayout.Width(18));
            if (selected) _selectedVariantIndex = i;

            _variantFoldouts[i] = EditorGUILayout.Foldout(_variantFoldouts[i], $"[{i}] {title} ({(ChargeLevel)charge})", true);

            EditorGUI.BeginDisabledGroup(i == 0);
            if (GUILayout.Button("Up", GUILayout.Width(36)))
                _variants.MoveArrayElement(i, i - 1);
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(i == _variants.arraySize - 1);
            if (GUILayout.Button("Down", GUILayout.Width(48)))
                _variants.MoveArrayElement(i, i + 1);
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(_variants.arraySize <= 1);
            if (GUILayout.Button("Del", GUILayout.Width(36)))
                deleteIndex = i;
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            if (_variantFoldouts[i])
            {
                EditorGUI.indentLevel++;
                DrawVariantFields(variant);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        if (deleteIndex >= 0)
        {
            _variants.DeleteArrayElementAtIndex(deleteIndex);
            _selectedVariantIndex = Mathf.Clamp(_selectedVariantIndex, 0, Mathf.Max(0, _variants.arraySize - 1));
            SyncFoldouts();
        }
    }

    private static void DrawVariantFields(SerializedProperty variant)
    {
        EditorGUILayout.LabelField("Basic", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(variant.FindPropertyRelative("_attackName"), new GUIContent("Attack Name"));
        EditorGUILayout.PropertyField(variant.FindPropertyRelative("_requiredCharge"), new GUIContent("Required Charge"));

        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField("Hits", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(variant.FindPropertyRelative("_hits"), true);

        var enableHoming = variant.FindPropertyRelative("_enableHoming");
        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField("Homing", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(enableHoming, new GUIContent("Enable Homing"));
        if (enableHoming.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(variant.FindPropertyRelative("_homingRadius"), new GUIContent("Radius"));
            EditorGUILayout.PropertyField(variant.FindPropertyRelative("_homingAngle"), new GUIContent("Angle"));
            EditorGUILayout.PropertyField(variant.FindPropertyRelative("_homingStrength"), new GUIContent("Strength"));
            EditorGUI.indentLevel--;
        }

        var enableMovement = variant.FindPropertyRelative("_enableMovement");
        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField("Movement", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(enableMovement, new GUIContent("Enable Movement"));
        if (enableMovement.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(variant.FindPropertyRelative("_moveCurve"), new GUIContent("Move Curve"));
            EditorGUILayout.PropertyField(variant.FindPropertyRelative("_moveDistance"), new GUIContent("Distance"));
            EditorGUILayout.PropertyField(variant.FindPropertyRelative("_moveSpeed"), new GUIContent("Speed"));
            EditorGUILayout.PropertyField(variant.FindPropertyRelative("_moveDuration"), new GUIContent("Duration"));
            EditorGUILayout.PropertyField(variant.FindPropertyRelative("_stopOnHit"), new GUIContent("Stop On Hit"));
            EditorGUILayout.PropertyField(variant.FindPropertyRelative("_isPhantom"), new GUIContent("Is Phantom"));
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField("Animation", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(variant.FindPropertyRelative("_animationStateName"), new GUIContent("State Name"));
        EditorGUILayout.PropertyField(variant.FindPropertyRelative("_transitionDuration"), new GUIContent("Transition Duration"));
        EditorGUILayout.PropertyField(variant.FindPropertyRelative("_chargeAnimationStateName"), new GUIContent("Charge State Name"));
        EditorGUILayout.PropertyField(variant.FindPropertyRelative("_chargeTransitionDuration"), new GUIContent("Charge Transition Duration"));
    }

    private void DrawPreviewControls()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Attack Preview", EditorStyles.boldLabel);
        _showPreview = GUILayout.Toggle(_showPreview, _showPreview ? "Visible" : "Hidden", "Button", GUILayout.Width(70));
        EditorGUILayout.EndHorizontal();

        if (_showPreview)
        {
            if (_variants != null && _variants.arraySize > 0)
            {
                _selectedVariantIndex = Mathf.Clamp(_selectedVariantIndex, 0, _variants.arraySize - 1);
                _selectedVariantIndex = EditorGUILayout.Popup("Preview Variant", _selectedVariantIndex, BuildVariantNames());
            }

            DrawPreviewTargetField();

            if (_previewTarget == null)
            {
                _previewAngle = EditorGUILayout.Slider("Direction", _previewAngle, 0f, 360f);
                _previewDirection = Quaternion.Euler(0, _previewAngle, 0) * Vector3.forward;
            }

            DrawLegend();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawPreviewTargetField()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();
        _previewTarget = (GameObject)EditorGUILayout.ObjectField("Target", _previewTarget, typeof(GameObject), true);
        if (EditorGUI.EndChangeCheck())
            _isAutoTarget = false;

        if (GUILayout.Button("Auto", GUILayout.Width(48)))
        {
            _isAutoTarget = true;
            TryAutoFindTarget();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawLegend()
    {
        var variant = GetSelectedVariant();
        if (variant == null) return;

        var hit = GetFirstHit(variant);

        EditorGUILayout.BeginHorizontal();
        DrawColorBox(AttackSphereOutline);
        GUILayout.Label("Attack", GUILayout.Width(54));

        if (variant.FindPropertyRelative("_enableHoming").boolValue)
        {
            DrawColorBox(HomingOutline);
            GUILayout.Label("Homing", GUILayout.Width(58));
        }

        if (hit != null && hit.FindPropertyRelative("EnableKnockback").boolValue)
        {
            DrawColorBox(KnockbackColor);
            GUILayout.Label("Knockback", GUILayout.Width(76));
        }

        if (variant.FindPropertyRelative("_enableMovement").boolValue)
        {
            DrawColorBox(MoveColor);
            GUILayout.Label("Move", GUILayout.Width(44));
        }

        EditorGUILayout.EndHorizontal();
    }

    private void HandleSceneGUI(SceneView sceneView)
    {
        if (!_showPreview) return;

        serializedObject.Update();
        GetPivotAndDirection(sceneView, out var pivot, out var dir);

        foreach (var t in targets)
        {
            var so = new SerializedObject(t);
            var variants = so.FindProperty("_variants");
            if (variants == null || variants.arraySize == 0) continue;

            int index = Mathf.Clamp(_selectedVariantIndex, 0, variants.arraySize - 1);
            DrawVariantPreview(pivot, dir, variants.GetArrayElementAtIndex(index));
        }
    }

    private static void DrawVariantPreview(Vector3 pivot, Vector3 dir, SerializedProperty variant)
    {
        var hit = GetFirstHit(variant);
        if (hit == null) return;

        float attackRange = hit.FindPropertyRelative("AttackRange").floatValue;
        float attackRadius = hit.FindPropertyRelative("AttackRadius").floatValue;
        float damageMultiplier = hit.FindPropertyRelative("DamageMultiplier").floatValue;
        bool isPhantom = variant.FindPropertyRelative("_isPhantom").boolValue;
        string attackName = variant.FindPropertyRelative("_attackName").stringValue;
        var attackPos = pivot + dir * attackRange;

        if (variant.FindPropertyRelative("_enableMovement").boolValue)
            DrawMovementPreview(pivot, dir, variant.FindPropertyRelative("_moveDistance").floatValue);

        if (variant.FindPropertyRelative("_enableHoming").boolValue)
        {
            DrawHomingPreview(
                pivot,
                dir,
                variant.FindPropertyRelative("_homingRadius").floatValue,
                variant.FindPropertyRelative("_homingAngle").floatValue);
        }

        DrawAttackSpherePreview(pivot, attackPos, attackRadius, isPhantom);

        if (hit.FindPropertyRelative("EnableKnockback").boolValue)
        {
            DrawKnockbackArrow(
                attackPos,
                dir,
                hit.FindPropertyRelative("KnockbackPower").floatValue,
                hit.FindPropertyRelative("KnockbackUpward").floatValue);
        }

        DrawInfoLabel(attackPos, attackRadius, attackName, attackRange, attackRadius, damageMultiplier, isPhantom);
    }

    private void TryAutoFindTarget()
    {
        if (!_isAutoTarget && _previewTarget != null) return;
        var executor = Object.FindFirstObjectByType<AttackExecutor>();
        _previewTarget = executor != null ? executor.gameObject : null;
    }

    private void GetPivotAndDirection(SceneView sceneView, out Vector3 pivot, out Vector3 dir)
    {
        if (_previewTarget != null)
        {
            pivot = _previewTarget.transform.position;
            dir = _previewTarget.transform.forward;
            return;
        }

        pivot = sceneView.camera.transform.position + sceneView.camera.transform.forward * 5f;
        pivot.y = 0f;
        dir = _previewDirection;
    }

    private string[] BuildVariantNames()
    {
        var names = new string[_variants.arraySize];
        for (int i = 0; i < _variants.arraySize; i++)
        {
            var variant = _variants.GetArrayElementAtIndex(i);
            var name = variant.FindPropertyRelative("_attackName").stringValue;
            var charge = (ChargeLevel)variant.FindPropertyRelative("_requiredCharge").intValue;
            names[i] = $"[{i}] {(string.IsNullOrEmpty(name) ? "Variant" : name)} ({charge})";
        }
        return names;
    }

    private void SyncFoldouts()
    {
        if (_variants == null) return;
        while (_variantFoldouts.Count < _variants.arraySize)
            _variantFoldouts.Add(true);
        while (_variantFoldouts.Count > _variants.arraySize)
            _variantFoldouts.RemoveAt(_variantFoldouts.Count - 1);
    }

    private SerializedProperty GetSelectedVariant()
    {
        if (_variants == null || _variants.arraySize == 0) return null;
        return _variants.GetArrayElementAtIndex(Mathf.Clamp(_selectedVariantIndex, 0, _variants.arraySize - 1));
    }

    private static SerializedProperty GetFirstHit(SerializedProperty variant)
    {
        var hits = variant.FindPropertyRelative("_hits");
        if (hits == null || hits.arraySize == 0) return null;
        return hits.GetArrayElementAtIndex(0);
    }

    private static void DrawColorBox(Color color)
    {
        var rect = GUILayoutUtility.GetRect(14, 14, GUILayout.Width(14), GUILayout.Height(14));
        EditorGUI.DrawRect(rect, color);
    }

    private static void DrawAttackSpherePreview(Vector3 pivot, Vector3 attackPos, float radius, bool isPhantom)
    {
        Handles.color = isPhantom ? new Color(0.7f, 0.3f, 1f, 0.18f) : AttackSphereColor;
        Handles.SphereHandleCap(0, attackPos, Quaternion.identity, radius * 2f, EventType.Repaint);

        Handles.color = isPhantom ? new Color(0.7f, 0.3f, 1f, 0.9f) : AttackSphereOutline;
        DrawWireSphere(attackPos, radius);

        Handles.color = new Color(1f, 0.4f, 0.4f, 0.6f);
        Handles.DrawDottedLine(pivot, attackPos, 4f);
    }

    private static void DrawHomingPreview(Vector3 pivot, Vector3 dir, float radius, float angle)
    {
        float halfAngle = angle * 0.5f;
        var startDir = Quaternion.Euler(0, -halfAngle, 0) * dir;

        Handles.color = HomingColor;
        Handles.DrawSolidArc(pivot, Vector3.up, startDir, angle, radius);

        Handles.color = HomingOutline;
        Handles.DrawWireArc(pivot, Vector3.up, startDir, angle, radius);
        Handles.DrawLine(pivot, pivot + Quaternion.Euler(0, -halfAngle, 0) * dir * radius);
        Handles.DrawLine(pivot, pivot + Quaternion.Euler(0, halfAngle, 0) * dir * radius);
    }

    private static void DrawMovementPreview(Vector3 pivot, Vector3 dir, float distance)
    {
        var endPos = pivot + dir * distance;
        Handles.color = MoveColor;
        Handles.DrawLine(pivot, endPos, 2f);
        DrawArrowHead(endPos, dir, 0.2f);
    }

    private static void DrawKnockbackArrow(Vector3 attackPos, Vector3 dir, float power, float upward)
    {
        var knockDir = (dir + Vector3.up * upward).normalized;
        float scale = Mathf.Clamp(power * 0.1f, 0.5f, 3f);
        Handles.color = KnockbackColor;
        Handles.DrawLine(attackPos, attackPos + knockDir * scale, 3f);
        DrawArrowHead(attackPos + knockDir * scale, knockDir, 0.2f);
    }

    private static void DrawInfoLabel(Vector3 attackPos, float radius, string name, float range, float attackRadius, float damageMultiplier, bool isPhantom)
    {
        var labelPos = attackPos + Vector3.up * (radius + 0.4f);
        string phantom = isPhantom ? " phantom" : string.Empty;
        string info = $"<b>{name}</b>{phantom}\nrange: {range:F1} radius: {attackRadius:F1}\ndamage: x{damageMultiplier:F2}";
        Handles.Label(labelPos, info, GetRichLabelStyle());
    }

    private static void DrawWireSphere(Vector3 center, float radius)
    {
        Handles.DrawWireDisc(center, Vector3.up, radius);
        Handles.DrawWireDisc(center, Vector3.right, radius);
        Handles.DrawWireDisc(center, Vector3.forward, radius);
    }

    private static void DrawArrowHead(Vector3 tip, Vector3 dir, float size)
    {
        if (dir == Vector3.zero) return;
        var right = Vector3.Cross(dir, Vector3.up).normalized;
        var back = -dir;
        Handles.DrawLine(tip, tip + (back + right) * size);
        Handles.DrawLine(tip, tip + (back - right) * size);
        var up = Vector3.Cross(dir, right).normalized;
        Handles.DrawLine(tip, tip + (back + up) * size);
        Handles.DrawLine(tip, tip + (back - up) * size);
    }

    private static GUIStyle _richLabelStyle;
    private static GUIStyle GetRichLabelStyle()
    {
        if (_richLabelStyle != null) return _richLabelStyle;
        _richLabelStyle = new GUIStyle(GUI.skin.label)
        {
            richText = true,
            fontSize = 11,
            alignment = TextAnchor.UpperCenter,
            padding = new RectOffset(4, 4, 2, 2),
        };
        _richLabelStyle.normal.textColor = Color.white;
        _richLabelStyle.normal.background = MakeTex(4, 4, new Color(0, 0, 0, 0.55f));
        return _richLabelStyle;
    }

    private static Texture2D MakeTex(int width, int height, Color color)
    {
        var pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = color;

        var texture = new Texture2D(width, height);
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }
}
#endif
