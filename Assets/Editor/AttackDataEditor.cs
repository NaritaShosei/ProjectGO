#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AttackData))]
[CanEditMultipleObjects]
public class AttackDataEditor : Editor
{
    // ---- SerializedProperty ----
    private SerializedProperty _attackId;
    private SerializedProperty _mode;
    private SerializedProperty _nextComboAttackId;
    private SerializedProperty _insertAfterAttackId;
    private SerializedProperty _isUnlockedBySkill;
    private SerializedProperty _requiredSkillId;
    private SerializedProperty _variants;

    // ---- プレビュー設定 ----
    private bool _showPreview = true;
    private int _selectedVariantIndex = 0;

    /// <summary>攻撃範囲の基点オブジェクト。</summary>
    private GameObject _previewTarget;

    /// <summary>
    /// true  = AttackExecutor の自動検索で設定された
    /// false = ユーザーが手動で設定した
    /// </summary>
    private bool _isAutoTarget;

    // オブジェクト未指定時の手動方向制御
    private Vector3 _previewDirection = Vector3.forward;
    private float _previewAngle = 0f;
    private bool _autoRotate = false;
    private double _lastTime;

    // バリアントリストの折りたたみ状態
    private List<bool> _variantFoldouts = new();

    // プレビューカラー
    private static readonly Color AttackSphereColor = new Color(1f, 0.3f, 0.3f, 0.25f);
    private static readonly Color AttackSphereOutline = new Color(1f, 0.2f, 0.2f, 0.9f);
    private static readonly Color HomingColor = new Color(0.3f, 0.8f, 1f, 0.15f);
    private static readonly Color HomingOutline = new Color(0.3f, 0.8f, 1f, 0.8f);
    private static readonly Color MoveColor = new Color(0.3f, 1f, 0.5f, 0.9f);
    private static readonly Color KnockbackColor = new Color(1f, 0.85f, 0.1f, 0.9f);

    // =========================================================
    //  Enable / Disable
    // =========================================================
    private void OnEnable()
    {
        _attackId = serializedObject.FindProperty("_attackId");
        _mode = serializedObject.FindProperty("_mode");
        _nextComboAttackId = serializedObject.FindProperty("_nextComboAttackId");
        _insertAfterAttackId = serializedObject.FindProperty("_insertAfterAttackId");
        _isUnlockedBySkill = serializedObject.FindProperty("_isUnlockedBySkill");
        _requiredSkillId = serializedObject.FindProperty("_requiredSkillId");
        _variants = serializedObject.FindProperty("_variants");

        _lastTime = EditorApplication.timeSinceStartup;

        SyncFoldouts();
        TryAutoFindTarget();
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        if (_autoRotate)
            EditorApplication.update -= Repaint;
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    // =========================================================
    //  自動検索
    // =========================================================
    private void TryAutoFindTarget()
    {
        if (!_isAutoTarget && _previewTarget != null) return;

        var executor = Object.FindFirstObjectByType<AttackExecutor>();
        if (executor != null)
        {
            _previewTarget = executor.gameObject;
            _isAutoTarget = true;
        }
        else
        {
            if (_isAutoTarget)
            {
                _previewTarget = null;
                _isAutoTarget = false;
            }
        }
    }

    // =========================================================
    //  Inspector GUI
    // =========================================================
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("CUSTOM EDITOR ACTIVE", MessageType.Warning);
        serializedObject.Update();

        // ---- プレビューコントロール ----
        DrawPreviewControls();
        EditorGUILayout.Space(4);

        // ---- 基本情報 ----
        EditorGUILayout.LabelField("基本情報", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(_attackId, new GUIContent("AttackId"));
        EditorGUILayout.PropertyField(_mode, new GUIContent("Mode"));
        EditorGUI.indentLevel--;
        EditorGUILayout.Space(2);

        // ---- Combo ----
        EditorGUILayout.LabelField("Combo", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(_nextComboAttackId, new GUIContent("次のコンボ攻撃ID", "次のコンボ攻撃ID。-1の場合はコンボ終了。"));
        EditorGUILayout.PropertyField(_insertAfterAttackId, new GUIContent("差し込み起点ID", "この差し込み攻撃を発動する起点AttackDataのID。-1で無効。"));
        EditorGUI.indentLevel--;
        EditorGUILayout.Space(2);

        // ---- Skill Unlock ----
        EditorGUILayout.LabelField("Skill Unlock", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(_isUnlockedBySkill, new GUIContent("スキル解放が必要"));
        if (_isUnlockedBySkill.boolValue)
        {
            EditorGUILayout.PropertyField(_requiredSkillId, new GUIContent("必要スキルID"));
        }
        EditorGUI.indentLevel--;
        EditorGUILayout.Space(4);

        // ---- 攻撃バリアント ----
        DrawVariantsList();

        serializedObject.ApplyModifiedProperties();

        // 自動回転
        if (_autoRotate && _previewTarget == null)
        {
            double now = EditorApplication.timeSinceStartup;
            float dt = (float)(now - _lastTime);
            _lastTime = now;
            _previewAngle = (_previewAngle + dt * 60f) % 360f;
            _previewDirection = Quaternion.Euler(0, _previewAngle, 0) * Vector3.forward;
            Repaint();
            SceneView.RepaintAll();
        }
    }

    // =========================================================
    //  バリアントリスト描画
    // =========================================================
    private void DrawVariantsList()
    {
        SyncFoldouts();

        // ヘッダー
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        EditorGUILayout.LabelField("攻撃バリアント", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("＋ 追加", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            serializedObject.ApplyModifiedProperties();

            var data = (AttackData)target;
            Undo.RecordObject(data, "Add AttackVariant");
            var variant = new AttackVariantData();
            variant.SetDefaults();
            data.AddVariant(variant);
            EditorUtility.SetDirty(data);

            serializedObject.Update();
            SyncFoldouts();
            _selectedVariantIndex = _variants.arraySize - 1;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(2);

        if (_variants.arraySize == 0)
        {
            EditorGUILayout.HelpBox("バリアントがありません。「＋ 追加」ボタンで追加してください。", MessageType.Info);
            return;
        }

        // バリアントごとに描画
        int deleteIndex = -1;
        for (int i = 0; i < _variants.arraySize; i++)
        {
            var variantProp = _variants.GetArrayElementAtIndex(i);
            bool isSelected = (i == _selectedVariantIndex);

            // 折りたたみヘッダー
            var nameProp = variantProp.FindPropertyRelative("_attackName");
            var chargeProp = variantProp.FindPropertyRelative("_requiredCharge");
            string label = string.IsNullOrEmpty(nameProp.stringValue)
                ? $"バリアント {i}"
                : nameProp.stringValue;
            string chargeLabel = GetChargeLevelLabel(chargeProp.intValue);

            // 背景色（選択中バリアント）
            var bgColor = GUI.backgroundColor;
            if (isSelected)
                GUI.backgroundColor = new Color(0.6f, 0.9f, 1f, 1f);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = bgColor;

            // ヘッダー行
            EditorGUILayout.BeginHorizontal();

            // プレビュー選択ラジオ
            bool wasSelected = isSelected;
            bool nowSelected = GUILayout.Toggle(isSelected, "", EditorStyles.radioButton, GUILayout.Width(16));
            if (nowSelected && !wasSelected)
            {
                _selectedVariantIndex = i;
                SceneView.RepaintAll();
            }

            // 折りたたみ
            _variantFoldouts[i] = EditorGUILayout.Foldout(
                _variantFoldouts[i],
                $"[{i}] {label}  ({chargeLabel})",
                true,
                EditorStyles.foldoutHeader);

            // 上へ / 下へ
            EditorGUI.BeginDisabledGroup(i == 0);
            if (GUILayout.Button("↑", GUILayout.Width(22)))
                _variants.MoveArrayElement(i, i - 1);
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(i == _variants.arraySize - 1);
            if (GUILayout.Button("↓", GUILayout.Width(22)))
                _variants.MoveArrayElement(i, i + 1);
            EditorGUI.EndDisabledGroup();

            // 削除ボタン
            var deleteColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("✕", GUILayout.Width(22)))
                deleteIndex = i;
            GUI.backgroundColor = deleteColor;

            EditorGUILayout.EndHorizontal();

            // 展開時：フィールド描画
            if (_variantFoldouts[i])
            {
                EditorGUI.indentLevel++;
                DrawVariantFields(variantProp);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        // 削除処理
        if (deleteIndex >= 0)
        {
            _variants.DeleteArrayElementAtIndex(deleteIndex);
            if (_selectedVariantIndex >= _variants.arraySize)
                _selectedVariantIndex = Mathf.Max(0, _variants.arraySize - 1);
            SyncFoldouts();
        }
    }

    private void DrawVariantFields(SerializedProperty variantProp)
    {
        // ---- 基本情報 ----
        EditorGUILayout.LabelField("基本情報", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(variantProp.FindPropertyRelative("_attackName"), new GUIContent("攻撃名"));
        EditorGUILayout.PropertyField(variantProp.FindPropertyRelative("_requiredCharge"), new GUIContent("必要溜めレベル"));
        EditorGUILayout.Space(2);

        // ---- ダメージ ----
        EditorGUILayout.LabelField("ダメージ", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(variantProp.FindPropertyRelative("_damageMultiplier"), new GUIContent("ダメージ倍率"));
        EditorGUILayout.Space(2);

        // ---- 攻撃範囲 ----
        EditorGUILayout.LabelField("攻撃範囲", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(variantProp.FindPropertyRelative("_attackRange"), new GUIContent("射程距離"));
        EditorGUILayout.PropertyField(variantProp.FindPropertyRelative("_attackRadius"), new GUIContent("当たり判定半径"));
        EditorGUILayout.Space(2);

        // ---- ノックバック ----
        var enableKnockback = variantProp.FindPropertyRelative("_enableKnockback");
        EditorGUILayout.LabelField("ノックバック", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(enableKnockback, new GUIContent("ノックバック有効"));
        if (enableKnockback.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(variantProp.FindPropertyRelative("_knockbackPower"), new GUIContent("強さ"));
            EditorGUILayout.PropertyField(variantProp.FindPropertyRelative("_knockbackUpward"), new GUIContent("垂直成分"));
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space(2);

        // ---- ホーミング ----
        var enableHoming = variantProp.FindPropertyRelative("_enableHoming");
        EditorGUILayout.LabelField("ホーミング", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(enableHoming, new GUIContent("ホーミング有効"));
        if (enableHoming.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(variantProp.FindPropertyRelative("_homingRadius"), new GUIContent("探索半径"));
            EditorGUILayout.PropertyField(variantProp.FindPropertyRelative("_homingAngle"), new GUIContent("探索角度"));
            EditorGUILayout.PropertyField(variantProp.FindPropertyRelative("_homingStrength"), new GUIContent("強さ"));
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space(2);

        // ---- 移動 ----
        var enableMovement = variantProp.FindPropertyRelative("_enableMovement");
        EditorGUILayout.LabelField("移動", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(enableMovement, new GUIContent("移動有効"));
        if (enableMovement.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(variantProp.FindPropertyRelative("_moveCurve"), new GUIContent("移動カーブ"));
            EditorGUILayout.PropertyField(variantProp.FindPropertyRelative("_moveDistance"), new GUIContent("移動距離"));
            EditorGUILayout.PropertyField(variantProp.FindPropertyRelative("_moveSpeed"), new GUIContent("移動速度"));
            EditorGUILayout.PropertyField(variantProp.FindPropertyRelative("_moveDuration"), new GUIContent("移動時間"));
            EditorGUILayout.PropertyField(variantProp.FindPropertyRelative("_stopOnHit"), new GUIContent("ヒット時停止"));
            EditorGUILayout.PropertyField(variantProp.FindPropertyRelative("_isPhantom"), new GUIContent("すり抜け攻撃"));
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space(2);

        // ---- ヒットストップ ----
        EditorGUILayout.LabelField("ヒットストップ", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(variantProp.FindPropertyRelative("_hitStopData"), new GUIContent("ヒットストップ設定"), true);
        EditorGUILayout.Space(2);

        // ---- アニメーション ----
        EditorGUILayout.LabelField("アニメーション", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(variantProp.FindPropertyRelative("_animationStateName"), new GUIContent("ステート名"));
        EditorGUILayout.PropertyField(variantProp.FindPropertyRelative("_transitionDuration"), new GUIContent("遷移時間 (-1=デフォルト)"));
        EditorGUILayout.PropertyField(variantProp.FindPropertyRelative("_chargeAnimationStateName"), new GUIContent("チャージ用ステート名"));
        EditorGUILayout.PropertyField(variantProp.FindPropertyRelative("_chargeTransitionDuration"), new GUIContent("チャージ遷移時間 (-1=デフォルト)"));
        EditorGUILayout.Space(2);

        // ---- サウンド ----
        EditorGUILayout.LabelField("サウンド", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(variantProp.FindPropertyRelative("_playGroundHitSE"), new GUIContent("地面ヒットSE"));
        EditorGUILayout.Space(2);

        // ---- 雷の追加ダメージ ----
        EditorGUILayout.LabelField("雷の追加ダメージ", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(variantProp.FindPropertyRelative("_additionalLightningDamages"),
            new GUIContent("追加ダメージデータ"), true);
    }

    // =========================================================
    //  プレビューコントロール（Inspector 上部）
    // =========================================================
    private void DrawPreviewControls()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // タイトル行
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("⚔  攻撃範囲プレビュー", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        _showPreview = GUILayout.Toggle(
            _showPreview, _showPreview ? "表示中" : "非表示",
            "Button", GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();

        if (_showPreview)
        {
            EditorGUILayout.Space(2);

            // バリアント選択（プレビュー対象）
            if (_variants != null && _variants.arraySize > 0)
            {
                var names = BuildVariantNames();
                _selectedVariantIndex = Mathf.Clamp(_selectedVariantIndex, 0, names.Length - 1);
                EditorGUI.BeginChangeCheck();
                _selectedVariantIndex = EditorGUILayout.Popup(
                    new GUIContent("プレビュー対象バリアント", "SceneViewに表示するバリアントを選択"),
                    _selectedVariantIndex, names);
                if (EditorGUI.EndChangeCheck())
                    SceneView.RepaintAll();
            }
            else
            {
                EditorGUILayout.HelpBox("バリアントが存在しません。", MessageType.Info);
            }

            EditorGUILayout.Space(2);

            // 基点オブジェクト
            if (_isAutoTarget)
                DrawAutoTargetField();
            else
                DrawManualTargetField();

            // 方向コントロール
            if (_previewTarget != null)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.LabelField("方向", $"{_previewTarget.name} の transform.forward を使用");
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                DrawDirectionControls();
            }

            EditorGUILayout.Space(2);
            DrawLegend();
        }

        EditorGUILayout.EndVertical();
    }

    private string[] BuildVariantNames()
    {
        var names = new string[_variants.arraySize];
        for (int i = 0; i < _variants.arraySize; i++)
        {
            var v = _variants.GetArrayElementAtIndex(i);
            var n = v.FindPropertyRelative("_attackName").stringValue;
            var c = v.FindPropertyRelative("_requiredCharge").intValue;
            names[i] = $"[{i}] {(string.IsNullOrEmpty(n) ? "バリアント" + i : n)} ({GetChargeLevelLabel(c)})";
        }
        return names;
    }

    private void DrawAutoTargetField()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(new GUIContent("基点オブジェクト", "AttackExecutor を持つオブジェクトを自動検索で設定しました。"));
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.ObjectField(_previewTarget, typeof(GameObject), true);
        EditorGUI.EndDisabledGroup();
        if (GUILayout.Button("手動", GUILayout.Width(42)))
        {
            _isAutoTarget = false;
            SceneView.RepaintAll();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.HelpBox(
            "AttackExecutor を自動検索して設定しました。別のオブジェクトを使う場合は「手動」ボタンで切り替えてください。",
            MessageType.Info);
        if (GUILayout.Button("再検索 (AttackExecutor)"))
        {
            _previewTarget = null;
            _isAutoTarget = false;
            TryAutoFindTarget();
            SceneView.RepaintAll();
        }
    }

    private void DrawManualTargetField()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();
        _previewTarget = (GameObject)EditorGUILayout.ObjectField(
            new GUIContent("基点オブジェクト",
                "シーン上のGameObjectを指定すると、そのオブジェクトの position / transform.forward を攻撃範囲の基点・方向として使用します。"),
            _previewTarget, typeof(GameObject), allowSceneObjects: true);
        if (EditorGUI.EndChangeCheck())
        {
            _isAutoTarget = false;
            if (_previewTarget != null && _autoRotate)
            {
                _autoRotate = false;
                EditorApplication.update -= Repaint;
            }
            SceneView.RepaintAll();
        }
        if (GUILayout.Button("自動検索", GUILayout.Width(62)))
        {
            _previewTarget = null;
            _isAutoTarget = false;
            TryAutoFindTarget();
            SceneView.RepaintAll();
        }
        EditorGUILayout.EndHorizontal();

        if (_previewTarget == null)
        {
            EditorGUILayout.HelpBox(
                "AttackExecutor を持つオブジェクトがシーンに存在しません。\n手動でオブジェクトを指定するか、シーンに配置してから「自動検索」を押してください。",
                MessageType.Warning);
        }
    }

    private void DrawDirectionControls()
    {
        EditorGUI.BeginChangeCheck();
        _previewAngle = EditorGUILayout.Slider("プレビュー方向 (Y回転)", _previewAngle, 0f, 360f);
        if (EditorGUI.EndChangeCheck())
        {
            _previewDirection = Quaternion.Euler(0, _previewAngle, 0) * Vector3.forward;
            SceneView.RepaintAll();
        }
        EditorGUI.BeginChangeCheck();
        _autoRotate = EditorGUILayout.Toggle("自動回転", _autoRotate);
        if (EditorGUI.EndChangeCheck())
        {
            _lastTime = EditorApplication.timeSinceStartup;
            if (_autoRotate) EditorApplication.update += Repaint;
            else EditorApplication.update -= Repaint;
        }
    }

    private void DrawLegend()
    {
        var variant = GetSelectedVariant();
        if (variant == null) return;

        EditorGUILayout.BeginHorizontal();
        DrawColorBox(AttackSphereOutline); GUILayout.Label("攻撃範囲", GUILayout.Width(60));

        if (variant.FindPropertyRelative("_enableHoming").boolValue)
        {
            DrawColorBox(HomingOutline); GUILayout.Label("ホーミング", GUILayout.Width(70));
        }
        if (variant.FindPropertyRelative("_enableKnockback").boolValue)
        {
            DrawColorBox(KnockbackColor); GUILayout.Label("ノックバック", GUILayout.Width(76));
        }
        if (variant.FindPropertyRelative("_enableMovement").boolValue)
        {
            DrawColorBox(MoveColor); GUILayout.Label("移動", GUILayout.Width(40));
        }
        EditorGUILayout.EndHorizontal();
    }

    private static void DrawColorBox(Color c)
    {
        var rect = GUILayoutUtility.GetRect(14, 14, GUILayout.Width(14), GUILayout.Height(14));
        EditorGUI.DrawRect(rect, c);
    }

    // =========================================================
    //  Scene GUI
    // =========================================================
    private void OnSceneGUI(SceneView sceneView)
    {
        if (!_showPreview) return;

        serializedObject.Update();

        GetPivotAndDirection(sceneView, out var pivot, out var dir);
        Handles.matrix = Matrix4x4.identity;

        // 複数選択でも全 target を描画
        foreach (var t in targets)
        {
            var so = new SerializedObject(t);
            var variantsProp = so.FindProperty("_variants");
            if (variantsProp == null || variantsProp.arraySize == 0) continue;

            int idx = Mathf.Clamp(_selectedVariantIndex, 0, variantsProp.arraySize - 1);
            var variantProp = variantsProp.GetArrayElementAtIndex(idx);

            DrawVariantPreview(pivot, dir, variantProp);
        }
    }

    private void DrawVariantPreview(Vector3 pivot, Vector3 dir, SerializedProperty v)
    {
        float attackRange = v.FindPropertyRelative("_attackRange").floatValue;
        float attackRadius = v.FindPropertyRelative("_attackRadius").floatValue;
        float damageMulti = v.FindPropertyRelative("_damageMultiplier").floatValue;
        bool isPhantom = v.FindPropertyRelative("_isPhantom").boolValue;
        string attackName = v.FindPropertyRelative("_attackName").stringValue;

        var attackPos = pivot + dir * attackRange;

        // 移動プレビュー
        bool enableMovement = v.FindPropertyRelative("_enableMovement").boolValue;
        float moveDistance = v.FindPropertyRelative("_moveDistance").floatValue;
        if (enableMovement)
            DrawMovementPreview(pivot, dir, moveDistance);

        // ホーミングプレビュー
        bool enableHoming = v.FindPropertyRelative("_enableHoming").boolValue;
        float homingRadius = v.FindPropertyRelative("_homingRadius").floatValue;
        float homingAngle = v.FindPropertyRelative("_homingAngle").floatValue;
        if (enableHoming)
            DrawHomingPreview(pivot, dir, homingRadius, homingAngle);

        // 攻撃球プレビュー
        DrawAttackSpherePreview(pivot, attackPos, attackRadius, isPhantom);

        // ノックバック矢印
        bool enableKnockback = v.FindPropertyRelative("_enableKnockback").boolValue;
        float knockbackPower = v.FindPropertyRelative("_knockbackPower").floatValue;
        float knockbackUpward = v.FindPropertyRelative("_knockbackUpward").floatValue;
        if (enableKnockback)
            DrawKnockbackArrow(attackPos, dir, knockbackPower, knockbackUpward);

        // 情報ラベル
        DrawInfoLabel(attackPos, attackRadius, attackName, attackRange, attackRadius, damageMulti, isPhantom);
    }

    // =========================================================
    //  基点・方向の解決
    // =========================================================
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

    // =========================================================
    //  描画メソッド
    // =========================================================
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
        Handles.DrawLine(pivot, pivot + Quaternion.Euler(0, +halfAngle, 0) * dir * radius);
    }

    private static void DrawMovementPreview(Vector3 pivot, Vector3 dir, float moveDistance)
    {
        var endPos = pivot + dir * moveDistance;
        Handles.color = MoveColor;
        Handles.DrawLine(pivot, endPos, 2f);
        DrawArrowHead(endPos, dir, 0.2f);
        Handles.Label(
            Vector3.Lerp(pivot, endPos, 0.5f) + Vector3.up * 0.3f,
            $"移動: {moveDistance:F1}m",
            GetLabelStyle(MoveColor));
    }

    private static void DrawKnockbackArrow(Vector3 attackPos, Vector3 dir, float power, float upward)
    {
        var knockDir = (dir + Vector3.up * upward).normalized;
        float scale = Mathf.Clamp(power * 0.1f, 0.5f, 3f);

        Handles.color = KnockbackColor;
        Handles.DrawLine(attackPos, attackPos + knockDir * scale, 3f);
        DrawArrowHead(attackPos + knockDir * scale, knockDir, 0.2f);
    }

    private static void DrawInfoLabel(Vector3 attackPos, float radius,
        string name, float range, float attackRadius, float damageMulti, bool isPhantom)
    {
        var labelPos = attackPos + Vector3.up * (radius + 0.4f);
        string phantom = isPhantom ? " 【すり抜け】" : "";
        string info = $"<b>{name}</b>{phantom}\n"
                       + $"射程: {range:F1}  半径: {attackRadius:F1}\n"
                       + $"倍率: ×{damageMulti:F2}";
        Handles.Label(labelPos, info, GetRichLabelStyle());
    }

    // =========================================================
    //  ユーティリティ
    // =========================================================
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
        int idx = Mathf.Clamp(_selectedVariantIndex, 0, _variants.arraySize - 1);
        return _variants.GetArrayElementAtIndex(idx);
    }

    private static string GetChargeLevelLabel(int value) => value switch
    {
        0 => "溜めなし",
        1 => "溜め1",
        2 => "溜め2",
        3 => "溜め3",
        _ => $"Level{value}",
    };

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

    private static GUIStyle _labelStyle;
    private static GUIStyle GetLabelStyle(Color c)
    {
        _labelStyle ??= new GUIStyle(GUI.skin.label);
        _labelStyle.normal.textColor = c;
        _labelStyle.fontStyle = FontStyle.Bold;
        _labelStyle.fontSize = 11;
        return _labelStyle;
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
        };
        _richLabelStyle.normal.textColor = Color.white;
        _richLabelStyle.normal.background = MakeTex(4, 4, new Color(0, 0, 0, 0.55f));
        _richLabelStyle.padding = new RectOffset(4, 4, 2, 2);
        return _richLabelStyle;
    }

    private static Texture2D MakeTex(int w, int h, Color c)
    {
        var pix = new Color[w * h];
        for (int i = 0; i < pix.Length; i++) pix[i] = c;
        var tex = new Texture2D(w, h);
        tex.SetPixels(pix);
        tex.Apply();
        return tex;
    }
}
#endif
