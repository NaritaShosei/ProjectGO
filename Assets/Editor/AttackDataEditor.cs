#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AttackData))]
[CanEditMultipleObjects]
public class AttackDataEditor : Editor
{
    // ---- プロパティの参照 ----
    private SerializedProperty _attackId;
    private SerializedProperty _attackName;
    private SerializedProperty _mode;
    private SerializedProperty _attackType;
    private SerializedProperty _comboIndex;
    private SerializedProperty _requiredCharge;
    private SerializedProperty _damageMultiplier;
    private SerializedProperty _attackRange;
    private SerializedProperty _attackRadius;
    private SerializedProperty _nextComboAttackId;
    private SerializedProperty _enableKnockback;
    private SerializedProperty _knockbackPower;
    private SerializedProperty _knockbackUpward;
    private SerializedProperty _enableHoming;
    private SerializedProperty _homingRadius;
    private SerializedProperty _homingAngle;
    private SerializedProperty _homingStrength;
    private SerializedProperty _moveType;
    private SerializedProperty _moveDistance;
    private SerializedProperty _moveSpeed;
    private SerializedProperty _moveDuration;
    private SerializedProperty _stopOnHit;
    private SerializedProperty _isPhantom;

    // ---- プレビュー設定 ----
    private bool _showPreview = true;

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

    // プレビューカラー
    private static readonly Color AttackSphereColor = new Color(1f, 0.3f, 0.3f, 0.25f);
    private static readonly Color AttackSphereOutline = new Color(1f, 0.2f, 0.2f, 0.9f);
    private static readonly Color HomingColor = new Color(0.3f, 0.8f, 1f, 0.15f);
    private static readonly Color HomingOutline = new Color(0.3f, 0.8f, 1f, 0.8f);
    private static readonly Color MoveColor = new Color(0.3f, 1f, 0.5f, 0.9f);
    private static readonly Color KnockbackColor = new Color(1f, 0.85f, 0.1f, 0.9f);

    private void OnEnable()
    {
        _attackId = serializedObject.FindProperty("_attackId");
        _attackName = serializedObject.FindProperty("_attackName");
        _mode = serializedObject.FindProperty("_mode");
        _attackType = serializedObject.FindProperty("_attackType");
        _comboIndex = serializedObject.FindProperty("_comboIndex");
        _requiredCharge = serializedObject.FindProperty("_requiredCharge");
        _damageMultiplier = serializedObject.FindProperty("_damageMultiplier");
        _attackRange = serializedObject.FindProperty("_attackRange");
        _attackRadius = serializedObject.FindProperty("_attackRadius");
        _nextComboAttackId = serializedObject.FindProperty("_nextComboAttackId");
        _enableKnockback = serializedObject.FindProperty("_enableKnockback");
        _knockbackPower = serializedObject.FindProperty("_knockbackPower");
        _knockbackUpward = serializedObject.FindProperty("_knockbackUpward");
        _enableHoming = serializedObject.FindProperty("_enableHoming");
        _homingRadius = serializedObject.FindProperty("_homingRadius");
        _homingAngle = serializedObject.FindProperty("_homingAngle");
        _homingStrength = serializedObject.FindProperty("_homingStrength");
        _moveType = serializedObject.FindProperty("_moveType");
        _moveDistance = serializedObject.FindProperty("_moveDistance");
        _moveSpeed = serializedObject.FindProperty("_moveSpeed");
        _moveDuration = serializedObject.FindProperty("_moveDuration");
        _stopOnHit = serializedObject.FindProperty("_stopOnHit");
        _isPhantom = serializedObject.FindProperty("_isPhantom");

        _lastTime = EditorApplication.timeSinceStartup;

        // AttackExecutor を持つオブジェクトを自動検索
        TryAutoFindTarget();

        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        if (_autoRotate)
            EditorApplication.update -= Repaint;

        SceneView.duringSceneGui -= OnSceneGUI;
    }

    /// <summary>
    /// シーン内の AttackExecutor を検索して _previewTarget に自動セットする。
    /// すでに手動で設定済みの場合は上書きしない。
    /// </summary>
    private void TryAutoFindTarget()
    {
        // 手動設定済みなら上書きしない
        if (!_isAutoTarget && _previewTarget != null) return;

        var executor = Object.FindObjectOfType<AttackExecutor>();
        if (executor != null)
        {
            _previewTarget = executor.gameObject;
            _isAutoTarget = true;
        }
        else
        {
            // 見つからなかった場合は自動フラグのみリセット（手動設定は維持）
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
        serializedObject.Update();

        DrawPreviewControls();
        EditorGUILayout.Space(4);

        DrawDefaultInspector();

        serializedObject.ApplyModifiedProperties();

        // 自動回転の更新（オブジェクト未指定時のみ有効）
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
    //  Inspector 上部のプレビューコントロール
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

            // ---- 基点オブジェクト ----
            if (_isAutoTarget)
            {
                // 自動検索で設定済み → 読み取り専用表示 ＋ 再検索ボタン
                DrawAutoTargetField();
            }
            else
            {
                // 手動フィールド
                DrawManualTargetField();
            }

            // ---- 方向コントロール ----
            if (_previewTarget != null)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.LabelField(
                    "方向",
                    $"{_previewTarget.name} の transform.forward を使用");
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

    /// <summary>自動検索で基点が見つかった場合の表示</summary>
    private void DrawAutoTargetField()
    {
        EditorGUILayout.BeginHorizontal();

        // ラベル＋アイコン
        var labelContent = new GUIContent(
            "基点オブジェクト",
            "AttackExecutor を持つオブジェクトを自動検索で設定しました。");
        EditorGUILayout.PrefixLabel(labelContent);

        // 読み取り専用のオブジェクトフィールド
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.ObjectField(_previewTarget, typeof(GameObject), true);
        EditorGUI.EndDisabledGroup();

        // 手動に切り替えるボタン
        if (GUILayout.Button("手動", GUILayout.Width(42)))
        {
            _isAutoTarget = false;
            // 自動で入っていたオブジェクトを手動フィールドの初期値として引き継ぐ
            SceneView.RepaintAll();
        }

        EditorGUILayout.EndHorizontal();

        // 自動設定であることを示すヘルプボックス
        EditorGUILayout.HelpBox(
            "AttackExecutor を自動検索して設定しました。\n" +
            "別のオブジェクトを使う場合は「手動」ボタンで切り替えてください。",
            MessageType.Info);

        // 再検索ボタン（シーンで差し替えたときなど）
        if (GUILayout.Button("再検索 (AttackExecutor)"))
        {
            _previewTarget = null;
            _isAutoTarget = false;
            TryAutoFindTarget();
            SceneView.RepaintAll();
        }
    }

    /// <summary>手動設定フィールド</summary>
    private void DrawManualTargetField()
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUI.BeginChangeCheck();
        _previewTarget = (GameObject)EditorGUILayout.ObjectField(
            new GUIContent(
                "基点オブジェクト",
                "シーン上のGameObjectを指定すると、\n" +
                "そのオブジェクトの position / transform.forward を\n" +
                "攻撃範囲の基点・方向として使用します。"),
            _previewTarget,
            typeof(GameObject),
            allowSceneObjects: true);
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

        // 自動検索ボタン
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
                "AttackExecutor を持つオブジェクトがシーンに存在しません。\n" +
                "手動でオブジェクトを指定するか、シーンに配置してから「自動検索」を押してください。",
                MessageType.Warning);
        }
    }

    /// <summary>基点オブジェクトがない場合の方向スライダー</summary>
    private void DrawDirectionControls()
    {
        EditorGUI.BeginChangeCheck();
        _previewAngle = EditorGUILayout.Slider(
            "プレビュー方向 (Y回転)", _previewAngle, 0f, 360f);
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
        // 複数選択時は最初の target を参照
        var data = (AttackData)target;

        EditorGUILayout.BeginHorizontal();

        DrawColorBox(AttackSphereOutline);
        GUILayout.Label("攻撃範囲", GUILayout.Width(60));

        if (data.EnableHoming)
        {
            DrawColorBox(HomingOutline);
            GUILayout.Label("ホーミング", GUILayout.Width(70));
        }
        if ((AttackMoveType)_moveType.enumValueIndex != AttackMoveType.None)
        {
            DrawColorBox(MoveColor);
            GUILayout.Label("移動", GUILayout.Width(36));
        }
        if (data.EnableKnockback)
        {
            DrawColorBox(KnockbackColor);
            GUILayout.Label("ノックバック", GUILayout.Width(76));
        }

        EditorGUILayout.EndHorizontal();
    }

    private static void DrawColorBox(Color c)
    {
        var rect = GUILayoutUtility.GetRect(14, 14, GUILayout.Width(14), GUILayout.Height(14));
        EditorGUI.DrawRect(rect, c);
    }

    // =========================================================
    //  Scene GUI（シーンビュー上の描画）
    // =========================================================
    private void OnSceneGUI(SceneView sceneView)
    {
        if (!_showPreview) return;

        GetPivotAndDirection(sceneView, out var pivot, out var dir);

        Handles.matrix = Matrix4x4.identity;

        // 複数選択されている全ての AttackData を描画
        foreach (var t in targets)
        {
            var data = (AttackData)t;
            var attackPos = pivot + dir * data.AttackRange;

            DrawMovementPreview(pivot, dir, data);
            DrawHomingPreview(pivot, dir, data);
            DrawAttackSpherePreview(pivot, attackPos, data);
            DrawKnockbackArrow(attackPos, dir, data);
            DrawInfoLabel(attackPos, data);
        }
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

        // フォールバック
        pivot = sceneView.camera.transform.position
                + sceneView.camera.transform.forward * 5f;
        pivot.y = 0f;
        dir = _previewDirection;
    }

    // =========================================================
    //  描画メソッド
    // =========================================================

    private static void DrawAttackSpherePreview(Vector3 pivot, Vector3 attackPos, AttackData data)
    {
        Handles.color = data.IsPhantom
            ? new Color(0.7f, 0.3f, 1f, 0.18f)
            : AttackSphereColor;
        Handles.SphereHandleCap(0, attackPos, Quaternion.identity,
            data.AttackRadius * 2f, EventType.Repaint);

        Handles.color = data.IsPhantom
            ? new Color(0.7f, 0.3f, 1f, 0.9f)
            : AttackSphereOutline;
        DrawWireSphere(attackPos, data.AttackRadius);

        Handles.color = new Color(1f, 0.4f, 0.4f, 0.6f);
        Handles.DrawDottedLine(pivot, attackPos, 4f);
    }

    private static void DrawHomingPreview(Vector3 pivot, Vector3 dir, AttackData data)
    {
        if (!data.EnableHoming) return;

        float halfAngle = data.HomingAngle * 0.5f;

        Handles.color = HomingColor;
        Handles.DrawSolidArc(pivot, Vector3.up,
            Quaternion.Euler(0, -halfAngle, 0) * dir,
            data.HomingAngle, data.HomingRadius);

        Handles.color = HomingOutline;
        Handles.DrawWireArc(pivot, Vector3.up,
            Quaternion.Euler(0, -halfAngle, 0) * dir,
            data.HomingAngle, data.HomingRadius);
        Handles.DrawLine(pivot,
            pivot + Quaternion.Euler(0, -halfAngle, 0) * dir * data.HomingRadius);
        Handles.DrawLine(pivot,
            pivot + Quaternion.Euler(0, halfAngle, 0) * dir * data.HomingRadius);
    }

    private static void DrawMovementPreview(Vector3 pivot, Vector3 dir, AttackData data)
    {
        if (data.MoveType == AttackMoveType.None) return;

        var endPos = pivot + dir * data.MoveDistance;

        Handles.color = MoveColor;
        switch (data.MoveType)
        {
            case AttackMoveType.Dash:
                Handles.DrawLine(pivot, endPos);
                DrawArrowHead(endPos, dir, 0.3f);
                break;
            case AttackMoveType.Step:
                Handles.DrawLine(pivot, endPos);
                DrawArrowHead(endPos, dir, 0.2f);
                Handles.DrawWireDisc(pivot, Vector3.up, 0.15f);
                break;
            case AttackMoveType.Curve:
                var ctrl = pivot + dir * data.MoveDistance * 0.5f
                                 + Vector3.up * data.MoveDistance * 0.3f;
                Handles.DrawBezier(pivot, endPos, ctrl, endPos, MoveColor, null, 2f);
                DrawArrowHead(endPos, dir, 0.25f);
                break;
        }

        Handles.Label(
            Vector3.Lerp(pivot, endPos, 0.5f) + Vector3.up * 0.3f,
            $"移動: {data.MoveDistance:F1}m",
            GetLabelStyle(MoveColor));
    }

    private static void DrawKnockbackArrow(Vector3 attackPos, Vector3 dir, AttackData data)
    {
        if (!data.EnableKnockback) return;

        var knockDir = (dir + Vector3.up * data.KnockbackUpward).normalized;
        float scale = Mathf.Clamp(data.KnockbackPower * 0.1f, 0.5f, 3f);

        Handles.color = KnockbackColor;
        Handles.DrawLine(attackPos, attackPos + knockDir * scale, 3f);
        DrawArrowHead(attackPos + knockDir * scale, knockDir, 0.2f);
    }

    private static void DrawInfoLabel(Vector3 attackPos, AttackData data)
    {
        var labelPos = attackPos + Vector3.up * (data.AttackRadius + 0.4f);

        string phantom = data.IsPhantom ? " 【すり抜け】" : "";
        string info = $"<b>{data.AttackName}</b>{phantom}\n"
                    + $"射程: {data.AttackRange:F1}  半径: {data.AttackRadius:F1}\n"
                    + $"倍率: ×{data.DamageMultiplier:F2}";

        Handles.Label(labelPos, info, GetRichLabelStyle());
    }

    // =========================================================
    //  ヘルパー
    // =========================================================
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
