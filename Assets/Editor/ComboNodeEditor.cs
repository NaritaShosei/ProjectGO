using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// AttackDataのコンボをノードで視覚的に編集するエディター拡張
/// </summary>
public class ComboNodeEditor : EditorWindow
{
    private AttackDataRepository _repository;
    private List<ComboNode> _nodes = new List<ComboNode>();
    private ComboNode _selectedNode;
    private ComboNode _connectingNode;
    private Vector2 _panOffset = Vector2.zero;
    private Vector2 _drag;
    private bool _isDraggingCanvas = false;
    private bool _isDraggingNode = false;

    private const float NODE_WIDTH = 220f;
    private const float NODE_HEIGHT = 140f;
    private const float TOOLBAR_HEIGHT = 80f;

    [MenuItem("Window/Combo Node Editor")]
    public static void OpenWindow()
    {
        var window = GetWindow<ComboNodeEditor>();
        window.titleContent = new GUIContent("Combo Node Editor");
        window.minSize = new Vector2(800, 600);
        window.Show();
    }

    private void OnEnable()
    {
        LoadRepository();
    }

    private void OnGUI()
    {
        // ツールバー描画
        DrawToolbar();

        // キャンバス領域
        Rect canvasRect = new Rect(0, TOOLBAR_HEIGHT, position.width, position.height - TOOLBAR_HEIGHT);

        // イベント処理（ノードより先に）
        ProcessCanvasEvents(Event.current, canvasRect);

        // キャンバスをクリップ
        GUI.BeginGroup(canvasRect);

        // グリッド描画（パンオフセット適用）
        DrawGrid(20, 0.2f, Color.gray, canvasRect);
        DrawGrid(100, 0.4f, Color.gray, canvasRect);

        // 接続線描画
        DrawConnections();
        DrawConnectionLine(Event.current);

        // ノード描画
        DrawNodes();

        // ノードイベント処理
        ProcessNodeEvents(Event.current);

        GUI.EndGroup();

        // 操作説明
        DrawInstructions();

        if (GUI.changed) Repaint();
    }

    private void DrawToolbar()
    {
        GUILayout.BeginArea(new Rect(0, 0, position.width, TOOLBAR_HEIGHT));
        EditorGUILayout.BeginVertical(EditorStyles.toolbar);

        GUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();

        EditorGUI.BeginChangeCheck();
        _repository = (AttackDataRepository)EditorGUILayout.ObjectField(
            "Attack Repository",
            _repository,
            typeof(AttackDataRepository),
            false,
            GUILayout.Width(400)
        );

        if (EditorGUI.EndChangeCheck())
        {
            LoadRepository();
        }

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Auto Layout", GUILayout.Width(100)))
        {
            AutoLayout();
        }

        if (GUILayout.Button("Reset View", GUILayout.Width(100)))
        {
            _panOffset = Vector2.zero;
        }

        EditorGUILayout.EndHorizontal();

        GUILayout.Space(5);

        // 接続モード表示
        if (_connectingNode != null)
        {
            EditorGUILayout.HelpBox(
                $"接続モード: 「{_connectingNode.AttackData.name}」から接続先を選択してください",
                MessageType.Info
            );
        }

        EditorGUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void DrawInstructions()
    {
        GUILayout.BeginArea(new Rect(10, 100, 400, 400));
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("操作方法", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("・中クリック/Alt+左ドラッグ: キャンバス移動");
        EditorGUILayout.LabelField("・左クリック: ノード選択");
        EditorGUILayout.LabelField("・左ドラッグ: ノード移動");
        EditorGUILayout.LabelField("・右クリック: コンテキストメニュー");
        EditorGUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void DrawGrid(float gridSpacing, float gridOpacity, Color gridColor, Rect canvasRect)
    {
        int widthDivs = Mathf.CeilToInt(canvasRect.width / gridSpacing);
        int heightDivs = Mathf.CeilToInt(canvasRect.height / gridSpacing);

        Handles.BeginGUI();
        Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);

        Vector3 offset = new Vector3(_panOffset.x % gridSpacing, _panOffset.y % gridSpacing, 0);

        for (int i = 0; i < widthDivs + 1; i++)
        {
            Handles.DrawLine(
                new Vector3(gridSpacing * i, -gridSpacing, 0) + offset,
                new Vector3(gridSpacing * i, canvasRect.height + gridSpacing, 0f) + offset
            );
        }

        for (int j = 0; j < heightDivs + 1; j++)
        {
            Handles.DrawLine(
                new Vector3(-gridSpacing, gridSpacing * j, 0) + offset,
                new Vector3(canvasRect.width + gridSpacing, gridSpacing * j, 0f) + offset
            );
        }

        Handles.color = Color.white;
        Handles.EndGUI();
    }

    private void LoadRepository()
    {
        _nodes.Clear();

        if (_repository == null) return;

        // リポジトリから全てのAttackDataを取得してノードを生成
        var field = typeof(AttackDataRepository).GetField("_attackDatabase",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (field != null)
        {
            var database = field.GetValue(_repository) as List<AttackData>;
            if (database != null)
            {
                foreach (var attack in database)
                {
                    if (attack != null)
                    {
                        _nodes.Add(new ComboNode(attack, new Vector2(100, 100)));
                    }
                }
            }
        }

        AutoLayout();
    }

    private void AutoLayout()
    {
        if (_nodes.Count == 0) return;

        // モード別にグループ化
        var warriorNodes = _nodes.Where(n => n.AttackData.Mode == PlayerMode.Warrior).ToList();
        var thunderNodes = _nodes.Where(n => n.AttackData.Mode == PlayerMode.Thunder).ToList();

        // 配置
        LayoutNodes(warriorNodes, new Vector2(50, 50), "闘神モード");
        LayoutNodes(thunderNodes, new Vector2(50, 550), "雷神モード");
    }

    private void LayoutNodes(List<ComboNode> nodes, Vector2 startPos, string modeLabel)
    {
        // 攻撃タイプ別にソート
        var lightNodes = nodes.Where(n => n.AttackData.AttackType == AttackType.LightAttack)
            .OrderBy(n => n.AttackData.ComboIndex).ToList();
        var heavyNodes = nodes.Where(n => n.AttackData.AttackType == AttackType.HeavyAttack)
            .OrderBy(n => n.AttackData.ComboIndex).ToList();
        var dodgeNodes = nodes.Where(n => n.AttackData.AttackType == AttackType.DodgeAttack).ToList();

        float xOffset = 0;
        float yOffset = startPos.y;

        // 弱攻撃
        foreach (var node in lightNodes)
        {
            node.Position = new Vector2(startPos.x + xOffset, yOffset);
            xOffset += NODE_WIDTH + 30;
        }

        xOffset = 0;
        yOffset += NODE_HEIGHT + 30;

        // 強攻撃
        foreach (var node in heavyNodes)
        {
            node.Position = new Vector2(startPos.x + xOffset, yOffset);
            xOffset += NODE_WIDTH + 30;
        }

        xOffset = 0;
        yOffset += NODE_HEIGHT + 30;

        // 回避攻撃
        foreach (var node in dodgeNodes)
        {
            node.Position = new Vector2(startPos.x + xOffset, yOffset);
            xOffset += NODE_WIDTH + 30;
        }
    }

    private void DrawNodes()
    {
        if (_nodes != null)
        {
            foreach (var node in _nodes)
            {
                node.Draw(_selectedNode == node, _panOffset);
            }
        }
    }

    private void DrawConnections()
    {
        if (_nodes != null)
        {
            foreach (var node in _nodes)
            {
                if (node.AttackData.NextComboAttackId != -1)
                {
                    var nextNode = _nodes.FirstOrDefault(n => n.AttackData.AttackId == node.AttackData.NextComboAttackId);
                    if (nextNode != null)
                    {
                        DrawNodeConnection(
                            node.GetRect(_panOffset),
                            nextNode.GetRect(_panOffset),
                            Color.cyan
                        );
                    }
                }
            }
        }
    }

    private void DrawConnectionLine(Event e)
    {
        if (_connectingNode != null)
        {
            Handles.BeginGUI();
            Handles.color = Color.yellow;

            Rect connectingRect = _connectingNode.GetRect(_panOffset);
            Vector3 startPos = new Vector3(connectingRect.xMax, connectingRect.center.y, 0);

            Handles.DrawLine(
                startPos,
                new Vector3(e.mousePosition.x, e.mousePosition.y - TOOLBAR_HEIGHT, 0)
            );
            Handles.EndGUI();
            Repaint();
        }
    }

    private void DrawNodeConnection(Rect start, Rect end, Color color)
    {
        Vector3 startPos = new Vector3(start.xMax, start.center.y, 0);
        Vector3 endPos = new Vector3(end.xMin, end.center.y, 0);
        Vector3 startTan = startPos + Vector3.right * 50;
        Vector3 endTan = endPos + Vector3.left * 50;

        Handles.BeginGUI();
        Handles.color = color;
        Handles.DrawBezier(startPos, endPos, startTan, endTan, color, null, 3f);

        // 矢印を描画
        Vector3 direction = (endPos - startPos).normalized;
        Vector3 arrowPos = endPos - direction * 10;
        Handles.DrawLine(arrowPos, arrowPos + Quaternion.Euler(0, 0, 135) * direction * 10);
        Handles.DrawLine(arrowPos, arrowPos + Quaternion.Euler(0, 0, -135) * direction * 10);

        Handles.color = Color.white;
        Handles.EndGUI();
    }

    private void ProcessCanvasEvents(Event e, Rect canvasRect)
    {
        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button == 2 || (e.button == 0 && e.alt)) // 中クリックまたはAlt+左クリック
                {
                    _isDraggingCanvas = true;
                    e.Use();
                }
                else if (e.button == 1 && canvasRect.Contains(e.mousePosition))
                {
                    ProcessContextMenu(e.mousePosition - new Vector2(0, TOOLBAR_HEIGHT));
                    e.Use();
                }
                break;

            case EventType.MouseDrag:
                if (_isDraggingCanvas && (e.button == 2 || (e.button == 0 && e.alt)))
                {
                    _panOffset += e.delta;
                    e.Use();
                    Repaint();
                }
                break;

            case EventType.MouseUp:
                if (e.button == 2 || (e.button == 0 && e.alt))
                {
                    _isDraggingCanvas = false;
                }
                _isDraggingNode = false;
                break;

            case EventType.ScrollWheel:
                // 将来的にズーム機能を追加可能
                break;
        }
    }

    private void ProcessNodeEvents(Event e)
    {
        if (_nodes == null) return;
        if (_isDraggingCanvas) return;

        // マウス位置をキャンバス座標に変換
        Vector2 canvasMousePos = e.mousePosition - new Vector2(0, TOOLBAR_HEIGHT);

        // 逆順でチェック（上に描画されているノードを優先）
        for (int i = _nodes.Count - 1; i >= 0; i--)
        {
            ComboNode node = _nodes[i];
            Rect nodeRect = node.GetRect(_panOffset);

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0 && nodeRect.Contains(canvasMousePos))
                    {
                        if (_connectingNode != null)
                        {
                            // 接続モード中
                            if (_connectingNode != node)
                            {
                                SetComboConnection(_connectingNode.AttackData, node.AttackData.AttackId);
                            }
                            _connectingNode = null;
                        }
                        else
                        {
                            // 通常選択
                            _selectedNode = node;
                            _isDraggingNode = true;
                        }
                        e.Use();
                        GUI.changed = true;
                        return;
                    }
                    break;

                case EventType.MouseDrag:
                    if (e.button == 0 && _isDraggingNode && _selectedNode == node)
                    {
                        node.Drag(e.delta);
                        e.Use();
                        Repaint();
                        return;
                    }
                    break;
            }
        }

        // 背景クリックで選択解除
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            _selectedNode = null;
            GUI.changed = true;
        }
    }

    private void ProcessContextMenu(Vector2 mousePosition)
    {
        GenericMenu genericMenu = new GenericMenu();

        if (_selectedNode != null)
        {
            genericMenu.AddItem(
                new GUIContent("接続を開始"),
                false,
                () => {
                    _connectingNode = _selectedNode;
                    Repaint();
                }
            );

            if (_selectedNode.AttackData.NextComboAttackId != -1)
            {
                genericMenu.AddItem(
                    new GUIContent("接続を解除"),
                    false,
                    () => {
                        SetComboConnection(_selectedNode.AttackData, -1);
                        Repaint();
                    }
                );
            }
            else
            {
                genericMenu.AddDisabledItem(new GUIContent("接続を解除"));
            }

            genericMenu.AddSeparator("");

            genericMenu.AddItem(
                new GUIContent("アセットを選択"),
                false,
                () => {
                    Selection.activeObject = _selectedNode.AttackData;
                    EditorGUIUtility.PingObject(_selectedNode.AttackData);
                }
            );
        }
        else
        {
            genericMenu.AddDisabledItem(new GUIContent("ノードを選択してください"));
        }

        if (_connectingNode != null)
        {
            genericMenu.AddSeparator("");
            genericMenu.AddItem(
                new GUIContent("接続モードをキャンセル"),
                false,
                () => {
                    _connectingNode = null;
                    Repaint();
                }
            );
        }

        genericMenu.ShowAsContext();
    }

    private void SetComboConnection(AttackData from, int toId)
    {
        var so = new SerializedObject(from);
        so.FindProperty("_nextComboAttackId").intValue = toId;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(from);
        AssetDatabase.SaveAssets();
    }
}

/// <summary>
/// ノードを表すクラス
/// </summary>
public class ComboNode
{
    public Vector2 Position;
    public AttackData AttackData;

    private GUIStyle _style;
    private GUIStyle _selectedStyle;
    private GUIStyle _titleStyle;

    private const float NODE_WIDTH = 220f;
    private const float NODE_HEIGHT = 140f;

    public ComboNode(AttackData attackData, Vector2 position)
    {
        AttackData = attackData;
        Position = position;
    }

    public void Drag(Vector2 delta)
    {
        Position += delta;
    }

    public Rect GetRect(Vector2 panOffset)
    {
        return new Rect(Position + panOffset, new Vector2(NODE_WIDTH, NODE_HEIGHT));
    }

    public void Draw(bool isSelected, Vector2 panOffset)
    {
        InitializeStyles();

        Rect rect = GetRect(panOffset);
        GUIStyle currentStyle = isSelected ? _selectedStyle : _style;

        // ノード背景
        GUI.Box(rect, "", currentStyle);

        // モード別の色帯
        Rect colorBar = new Rect(rect.x, rect.y, rect.width, 5);
        EditorGUI.DrawRect(colorBar, GetModeColor(AttackData.Mode));

        GUILayout.BeginArea(rect);
        GUILayout.Space(8);

        // タイトル
        GUILayout.Label(AttackData.name, _titleStyle);

        GUILayout.Space(5);

        // 情報表示
        EditorGUILayout.BeginVertical();

        GUILayout.Label($"ID: {AttackData.AttackId}", EditorStyles.miniLabel);
        GUILayout.Label($"モード: {GetModeText(AttackData.Mode)}", EditorStyles.miniLabel);
        GUILayout.Label($"タイプ: {GetAttackTypeText(AttackData.AttackType)}", EditorStyles.miniLabel);
        GUILayout.Label($"コンボ段階: {AttackData.ComboIndex + 1}段目", EditorStyles.miniLabel);

        if (AttackData.RequiredCharge != ChargeLevel.None)
        {
            GUILayout.Label($"チャージ: {GetChargeText(AttackData.RequiredCharge)}", EditorStyles.miniLabel);
        }

        if (AttackData.NextComboAttackId != -1)
        {
            var style = new GUIStyle(EditorStyles.miniLabel);
            style.normal.textColor = Color.cyan;
            GUILayout.Label($"→ 次: ID {AttackData.NextComboAttackId}", style);
        }

        EditorGUILayout.EndVertical();

        GUILayout.EndArea();
    }

    private void InitializeStyles()
    {
        if (_style == null)
        {
            _style = new GUIStyle();
            _style.normal.background = MakeTex(2, 2, new Color(0.2f, 0.2f, 0.2f, 0.9f));
            _style.border = new RectOffset(1, 1, 1, 1);
        }

        if (_selectedStyle == null)
        {
            _selectedStyle = new GUIStyle(_style);
            _selectedStyle.normal.background = MakeTex(2, 2, new Color(0.3f, 0.5f, 0.7f, 0.9f));
        }

        if (_titleStyle == null)
        {
            _titleStyle = new GUIStyle(EditorStyles.boldLabel);
            _titleStyle.fontSize = 12;
            _titleStyle.alignment = TextAnchor.MiddleCenter;
        }
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;

        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    private Color GetModeColor(PlayerMode mode)
    {
        switch (mode)
        {
            case PlayerMode.Warrior: return new Color(1f, 0.3f, 0.3f);
            case PlayerMode.Thunder: return new Color(0.3f, 0.7f, 1f);
            default: return Color.gray;
        }
    }

    private string GetModeText(PlayerMode mode)
    {
        switch (mode)
        {
            case PlayerMode.Warrior: return "闘神";
            case PlayerMode.Thunder: return "雷神";
            default: return "不明";
        }
    }

    private string GetAttackTypeText(AttackType type)
    {
        switch (type)
        {
            case AttackType.LightAttack: return "弱攻撃";
            case AttackType.HeavyAttack: return "強攻撃";
            case AttackType.DodgeAttack: return "回避攻撃";
            default: return "不明";
        }
    }

    private string GetChargeText(ChargeLevel level)
    {
        switch (level)
        {
            case ChargeLevel.Level1: return "溜め1";
            case ChargeLevel.Level2: return "溜め2";
            default: return "なし";
        }
    }
}
