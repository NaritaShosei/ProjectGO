//using System.Collections.Generic;
//using System.Linq;
//using UnityEditor;
//using UnityEngine;

///// <summary>
///// AttackDataのコンボをノードで視覚的に編集するエディター拡張
///// </summary>
//public class ComboNodeEditor : EditorWindow
//{
//    private AttackDataRepository _repository;
//    private List<ComboNode> _nodes = new List<ComboNode>();
//    private ComboNode _selectedNode;
//    private ComboNode _connectingNode;
//    private ConnectionType _connectionType = ConnectionType.Next;
//    private Vector2 _panOffset = Vector2.zero;
//    private bool _isDraggingCanvas = false;
//    private bool _isDraggingNode = false;

//    private const float NODE_WIDTH = 220f;
//    private const float NODE_HEIGHT = 160f;
//    private const float TOOLBAR_HEIGHT = 80f;

//    private enum ConnectionType { Next, Fallback }

//    [MenuItem("Window/Combo Node Editor")]
//    public static void OpenWindow()
//    {
//        var window = GetWindow<ComboNodeEditor>();
//        window.titleContent = new GUIContent("Combo Node Editor");
//        window.minSize = new Vector2(800, 600);
//        window.Show();
//    }

//    private void OnEnable()
//    {
//        LoadRepository();
//    }

//    private void OnGUI()
//    {
//        DrawToolbar();

//        Rect canvasRect = new Rect(0, TOOLBAR_HEIGHT, position.width, position.height - TOOLBAR_HEIGHT);

//        ProcessCanvasEvents(Event.current, canvasRect);

//        GUI.BeginGroup(canvasRect);

//        DrawGrid(20, 0.2f, Color.gray, canvasRect);
//        DrawGrid(100, 0.4f, Color.gray, canvasRect);

//        DrawConnections();
//        DrawConnectionLine(Event.current);

//        DrawNodes();

//        ProcessNodeEvents(Event.current);

//        GUI.EndGroup();

//        DrawInstructions();

//        if (GUI.changed) Repaint();
//    }

//    private void DrawToolbar()
//    {
//        GUILayout.BeginArea(new Rect(0, 0, position.width, TOOLBAR_HEIGHT));
//        EditorGUILayout.BeginVertical(EditorStyles.toolbar);

//        GUILayout.Space(5);

//        EditorGUILayout.BeginHorizontal();

//        EditorGUI.BeginChangeCheck();
//        _repository = (AttackDataRepository)EditorGUILayout.ObjectField(
//            "Attack Repository",
//            _repository,
//            typeof(AttackDataRepository),
//            false,
//            GUILayout.Width(400)
//        );
//        if (EditorGUI.EndChangeCheck())
//        {
//            LoadRepository();
//        }

//        GUILayout.FlexibleSpace();

//        if (GUILayout.Button("Auto Layout", GUILayout.Width(100)))
//            AutoLayout();

//        if (GUILayout.Button("Reset View", GUILayout.Width(100)))
//            _panOffset = Vector2.zero;

//        EditorGUILayout.EndHorizontal();

//        GUILayout.Space(5);

//        if (_connectingNode != null)
//        {
//            string label = _connectionType == ConnectionType.Next
//                ? $"[次コンボ接続] 「{_connectingNode.AttackData.name}」から接続先を選択"
//                : $"[フォールバック接続] 「{_connectingNode.AttackData.name}」から接続先を選択";
//            EditorGUILayout.HelpBox(label, MessageType.Info);
//        }

//        EditorGUILayout.EndVertical();
//        GUILayout.EndArea();
//    }

//    private void DrawInstructions()
//    {
//        GUILayout.BeginArea(new Rect(10, TOOLBAR_HEIGHT + 10, 420, 120));
//        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
//        EditorGUILayout.LabelField("操作方法", EditorStyles.boldLabel);
//        EditorGUILayout.LabelField("・中クリック / Alt+左ドラッグ: キャンバス移動");
//        EditorGUILayout.LabelField("・左クリック: ノード選択 / 接続先確定");
//        EditorGUILayout.LabelField("・左ドラッグ: ノード移動");
//        EditorGUILayout.LabelField("・右クリック: コンテキストメニュー");
//        EditorGUILayout.LabelField("接続線: 水色=次コンボ  橙=フォールバック");
//        EditorGUILayout.EndVertical();
//        GUILayout.EndArea();
//    }

//    private void DrawGrid(float gridSpacing, float gridOpacity, Color gridColor, Rect canvasRect)
//    {
//        int widthDivs = Mathf.CeilToInt(canvasRect.width / gridSpacing);
//        int heightDivs = Mathf.CeilToInt(canvasRect.height / gridSpacing);

//        Handles.BeginGUI();
//        Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);

//        Vector3 offset = new Vector3(_panOffset.x % gridSpacing, _panOffset.y % gridSpacing, 0);

//        for (int i = 0; i < widthDivs + 1; i++)
//            Handles.DrawLine(
//                new Vector3(gridSpacing * i, -gridSpacing, 0) + offset,
//                new Vector3(gridSpacing * i, canvasRect.height + gridSpacing, 0) + offset);

//        for (int j = 0; j < heightDivs + 1; j++)
//            Handles.DrawLine(
//                new Vector3(-gridSpacing, gridSpacing * j, 0) + offset,
//                new Vector3(canvasRect.width + gridSpacing, gridSpacing * j, 0) + offset);

//        Handles.color = Color.white;
//        Handles.EndGUI();
//    }

//    private void LoadRepository()
//    {
//        _nodes.Clear();
//        if (_repository == null) return;

//        var field = typeof(AttackDataRepository).GetField(
//            "_attackDatabase",
//            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

//        if (field == null) return;

//        var database = field.GetValue(_repository) as List<AttackData>;
//        if (database == null) return;

//        foreach (var attack in database)
//        {
//            if (attack != null)
//                _nodes.Add(new ComboNode(attack, new Vector2(100, 100)));
//        }

//        AutoLayout();
//    }

//    private void AutoLayout()
//    {
//        if (_nodes.Count == 0) return;

//        var warriorNodes = _nodes.Where(n => n.AttackData.Mode == PlayerMode.Warrior).ToList();
//        var thunderNodes = _nodes.Where(n => n.AttackData.Mode == PlayerMode.Thunder).ToList();

//        LayoutWarriorNodes(warriorNodes, new Vector2(50, 50));
//        LayoutThunderNodes(thunderNodes, new Vector2(50, 600));
//    }

//    /// <summary>
//    /// 闘神ノードをチャージLv別に横並びでレイアウトする
//    /// Row0: チャージなし（コンボ順）
//    /// Row1: チャージLv1
//    /// Row2: チャージLv2
//    /// Row3: チャージLv3
//    /// </summary>
//    private void LayoutWarriorNodes(List<ComboNode> nodes, Vector2 startPos)
//    {
//        var groups = new Dictionary<ChargeLevel, List<ComboNode>>
//        {
//            { ChargeLevel.None,   nodes.Where(n => n.AttackData.RequiredCharge == ChargeLevel.None)
//                                       .OrderBy(n => n.AttackData.ComboIndex).ToList() },
//            { ChargeLevel.Level1, nodes.Where(n => n.AttackData.RequiredCharge == ChargeLevel.Level1)
//                                       .OrderBy(n => n.AttackData.ComboIndex).ToList() },
//            { ChargeLevel.Level2, nodes.Where(n => n.AttackData.RequiredCharge == ChargeLevel.Level2)
//                                       .OrderBy(n => n.AttackData.ComboIndex).ToList() },
//            { ChargeLevel.Level3, nodes.Where(n => n.AttackData.RequiredCharge == ChargeLevel.Level3)
//                                       .OrderBy(n => n.AttackData.ComboIndex).ToList() },
//        };

//        float rowY = startPos.y;
//        float rowSpacing = NODE_HEIGHT + 30f;

//        foreach (var kvp in groups)
//        {
//            float x = startPos.x;
//            foreach (var node in kvp.Value)
//            {
//                node.Position = new Vector2(x, rowY);
//                x += NODE_WIDTH + 30f;
//            }
//            if (kvp.Value.Count > 0)
//                rowY += rowSpacing;
//        }
//    }

//    /// <summary>
//    /// 雷神ノードをコンボ順（IsUnlockedBySkill で色分け）でレイアウトする
//    /// </summary>
//    private void LayoutThunderNodes(List<ComboNode> nodes, Vector2 startPos)
//    {
//        var sorted = nodes.OrderBy(n => n.AttackData.ComboIndex).ToList();
//        float x = startPos.x;
//        foreach (var node in sorted)
//        {
//            node.Position = new Vector2(x, startPos.y);
//            x += NODE_WIDTH + 30f;
//        }
//    }

//    private void DrawNodes()
//    {
//        if (_nodes == null) return;
//        foreach (var node in _nodes)
//            node.Draw(_selectedNode == node, _panOffset);
//    }

//    private void DrawConnections()
//    {
//        if (_nodes == null) return;

//        foreach (var node in _nodes)
//        {
//            // 次コンボ接続（水色）
//            if (node.AttackData.NextComboAttackId != -1)
//            {
//                var nextNode = _nodes.FirstOrDefault(n => n.AttackData.AttackId == node.AttackData.NextComboAttackId);
//                if (nextNode != null)
//                    DrawNodeConnection(node.GetRect(_panOffset), nextNode.GetRect(_panOffset), Color.cyan);
//            }
//        }
//    }

//    private void DrawConnectionLine(Event e)
//    {
//        if (_connectingNode == null) return;

//        Handles.BeginGUI();
//        Handles.color = _connectionType == ConnectionType.Next ? Color.yellow : new Color(1f, 0.8f, 0f);

//        Rect r = _connectingNode.GetRect(_panOffset);
//        Handles.DrawLine(
//            new Vector3(r.xMax, r.center.y, 0),
//            new Vector3(e.mousePosition.x, e.mousePosition.y - TOOLBAR_HEIGHT, 0));

//        Handles.color = Color.white;
//        Handles.EndGUI();
//        Repaint();
//    }

//    private void DrawNodeConnection(Rect start, Rect end, Color color)
//    {
//        Vector3 startPos = new Vector3(start.xMax, start.center.y, 0);
//        Vector3 endPos = new Vector3(end.xMin, end.center.y, 0);
//        Vector3 startTan = startPos + Vector3.right * 50;
//        Vector3 endTan = endPos + Vector3.left * 50;

//        Handles.BeginGUI();
//        Handles.DrawBezier(startPos, endPos, startTan, endTan, color, null, 3f);

//        // 矢印
//        Vector3 dir = (endPos - startPos).normalized;
//        Vector3 tip = endPos - dir * 10;
//        Handles.color = color;
//        Handles.DrawLine(tip, tip + Quaternion.Euler(0, 0, 135) * dir * 10);
//        Handles.DrawLine(tip, tip + Quaternion.Euler(0, 0, -135) * dir * 10);

//        Handles.color = Color.white;
//        Handles.EndGUI();
//    }

//    private void ProcessCanvasEvents(Event e, Rect canvasRect)
//    {
//        switch (e.type)
//        {
//            case EventType.MouseDown:
//                if (e.button == 2 || (e.button == 0 && e.alt))
//                {
//                    _isDraggingCanvas = true;
//                    e.Use();
//                }
//                else if (e.button == 1 && canvasRect.Contains(e.mousePosition))
//                {
//                    ProcessContextMenu(e.mousePosition - new Vector2(0, TOOLBAR_HEIGHT));
//                    e.Use();
//                }
//                break;

//            case EventType.MouseDrag:
//                if (_isDraggingCanvas && (e.button == 2 || (e.button == 0 && e.alt)))
//                {
//                    _panOffset += e.delta;
//                    e.Use();
//                    Repaint();
//                }
//                break;

//            case EventType.MouseUp:
//                if (e.button == 2 || (e.button == 0 && e.alt))
//                    _isDraggingCanvas = false;
//                _isDraggingNode = false;
//                break;
//        }
//    }

//    private void ProcessNodeEvents(Event e)
//    {
//        if (_nodes == null || _isDraggingCanvas) return;

//        Vector2 canvasMousePos = e.mousePosition; // GUI.BeginGroup内なのでそのまま使える

//        for (int i = _nodes.Count - 1; i >= 0; i--)
//        {
//            ComboNode node = _nodes[i];
//            Rect nodeRect = node.GetRect(_panOffset);

//            switch (e.type)
//            {
//                case EventType.MouseDown:
//                    if (e.button == 0 && nodeRect.Contains(canvasMousePos))
//                    {
//                        if (_connectingNode != null)
//                        {
//                            if (_connectingNode != node)
//                            {
//                                if (_connectionType == ConnectionType.Next)
//                                    SetConnection(_connectingNode.AttackData, "_nextComboAttackId", node.AttackData.AttackId);
//                                else
//                                    SetConnection(_connectingNode.AttackData, "_fallbackNextComboAttackId", node.AttackData.AttackId);
//                            }
//                            _connectingNode = null;
//                        }
//                        else
//                        {
//                            _selectedNode = node;
//                            _isDraggingNode = true;
//                        }
//                        e.Use();
//                        GUI.changed = true;
//                        return;
//                    }
//                    break;

//                case EventType.MouseDrag:
//                    if (e.button == 0 && _isDraggingNode && _selectedNode == node)
//                    {
//                        node.Drag(e.delta);
//                        e.Use();
//                        Repaint();
//                        return;
//                    }
//                    break;
//            }
//        }

//        if (e.type == EventType.MouseDown && e.button == 0)
//        {
//            _selectedNode = null;
//            GUI.changed = true;
//        }
//    }

//    private void ProcessContextMenu(Vector2 mousePosition)
//    {
//        var menu = new GenericMenu();

//        if (_selectedNode != null)
//        {
//            // 次コンボ接続
//            menu.AddItem(new GUIContent("次コンボ接続を開始"), false, () =>
//            {
//                _connectingNode = _selectedNode;
//                _connectionType = ConnectionType.Next;
//                Repaint();
//            });

//            if (_selectedNode.AttackData.NextComboAttackId != -1)
//                menu.AddItem(new GUIContent("次コンボ接続を解除"), false, () =>
//                {
//                    SetConnection(_selectedNode.AttackData, "_nextComboAttackId", -1);
//                    Repaint();
//                });
//            else
//                menu.AddDisabledItem(new GUIContent("次コンボ接続を解除"));

//            menu.AddSeparator("");

//            // フォールバック接続
//            menu.AddItem(new GUIContent("フォールバック接続を開始"), false, () =>
//            {
//                _connectingNode = _selectedNode;
//                _connectionType = ConnectionType.Fallback;
//                Repaint();
//            });

//            menu.AddSeparator("");

//            menu.AddItem(new GUIContent("アセットを選択"), false, () =>
//            {
//                Selection.activeObject = _selectedNode.AttackData;
//                EditorGUIUtility.PingObject(_selectedNode.AttackData);
//            });
//        }
//        else
//        {
//            menu.AddDisabledItem(new GUIContent("ノードを選択してください"));
//        }

//        if (_connectingNode != null)
//        {
//            menu.AddSeparator("");
//            menu.AddItem(new GUIContent("接続モードをキャンセル"), false, () =>
//            {
//                _connectingNode = null;
//                Repaint();
//            });
//        }

//        menu.ShowAsContext();
//    }

//    private void SetConnection(AttackData from, string propertyName, int toId)
//    {
//        var so = new SerializedObject(from);
//        var prop = so.FindProperty(propertyName);
//        if (prop == null)
//        {
//            Debug.LogWarning($"[ComboNodeEditor] プロパティ '{propertyName}' が見つかりません");
//            return;
//        }
//        prop.intValue = toId;
//        so.ApplyModifiedProperties();
//        EditorUtility.SetDirty(from);
//        AssetDatabase.SaveAssets();
//    }
//}

///// <summary>
///// ノードを表すクラス
///// </summary>
//public class ComboNode
//{
//    public Vector2 Position;
//    public AttackData AttackData;

//    private GUIStyle _style;
//    private GUIStyle _selectedStyle;
//    private GUIStyle _titleStyle;

//    private const float NODE_WIDTH = 220f;
//    private const float NODE_HEIGHT = 160f;

//    public ComboNode(AttackData attackData, Vector2 position)
//    {
//        AttackData = attackData;
//        Position = position;
//    }

//    public void Drag(Vector2 delta) => Position += delta;

//    public Rect GetRect(Vector2 panOffset) =>
//        new Rect(Position + panOffset, new Vector2(NODE_WIDTH, NODE_HEIGHT));

//    public void Draw(bool isSelected, Vector2 panOffset)
//    {
//        InitializeStyles();

//        Rect rect = GetRect(panOffset);
//        GUIStyle curStyle = isSelected ? _selectedStyle : _style;

//        GUI.Box(rect, "", curStyle);

//        // モード色帯（上）
//        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 5), GetModeColor(AttackData.Mode));

//        // スキル解放必要な場合は左端に色帯
//        if (AttackData.IsUnlockedBySkill)
//            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 4, rect.height), new Color(1f, 0.9f, 0f));

//        GUILayout.BeginArea(rect);
//        GUILayout.Space(9);

//        // タイトル
//        GUILayout.Label(AttackData.name, _titleStyle);

//        GUILayout.Space(3);

//        EditorGUILayout.BeginVertical();

//        GUILayout.Label($"ID: {AttackData.AttackId}", EditorStyles.miniLabel);
//        GUILayout.Label($"モード: {GetModeText(AttackData.Mode)}", EditorStyles.miniLabel);
//        GUILayout.Label($"チャージ: {GetChargeText(AttackData.RequiredCharge)}", EditorStyles.miniLabel);
//        GUILayout.Label($"コンボ段: {AttackData.ComboIndex + 1}段目", EditorStyles.miniLabel);

//        //// 雷追加ダメージ
//        //if (AttackData.LightningDamageMultiplier > 0f)
//        //{
//        //    var style = new GUIStyle(EditorStyles.miniLabel);
//        //    style.normal.textColor = new Color(0.3f, 0.7f, 1f);
//        //    GUILayout.Label($"⚡ 雷ダメージ x{AttackData.LightningDamageMultiplier:F2}", style);
//        //}

//        // 次コンボ
//        if (AttackData.NextComboAttackId != -1)
//        {
//            var style = new GUIStyle(EditorStyles.miniLabel);
//            style.normal.textColor = Color.cyan;
//            GUILayout.Label($"→ 次: ID {AttackData.NextComboAttackId}", style);
//        }

//        // スキル解放
//        if (AttackData.IsUnlockedBySkill)
//        {
//            var style = new GUIStyle(EditorStyles.miniLabel);
//            style.normal.textColor = new Color(1f, 0.9f, 0f);
//            GUILayout.Label($"🔒 スキルID: {AttackData.RequiredSkillId}", style);
//        }

//        EditorGUILayout.EndVertical();

//        GUILayout.EndArea();
//    }

//    private void InitializeStyles()
//    {
//        if (_style == null)
//        {
//            _style = new GUIStyle();
//            _style.normal.background = MakeTex(2, 2, new Color(0.2f, 0.2f, 0.2f, 0.9f));
//            _style.border = new RectOffset(1, 1, 1, 1);
//        }

//        if (_selectedStyle == null)
//        {
//            _selectedStyle = new GUIStyle(_style);
//            _selectedStyle.normal.background = MakeTex(2, 2, new Color(0.3f, 0.5f, 0.7f, 0.9f));
//        }

//        if (_titleStyle == null)
//        {
//            _titleStyle = new GUIStyle(EditorStyles.boldLabel);
//            _titleStyle.fontSize = 12;
//            _titleStyle.alignment = TextAnchor.MiddleCenter;
//        }
//    }

//    private Texture2D MakeTex(int width, int height, Color col)
//    {
//        var pix = new Color[width * height];
//        for (int i = 0; i < pix.Length; i++) pix[i] = col;
//        var tex = new Texture2D(width, height);
//        tex.SetPixels(pix);
//        tex.Apply();
//        return tex;
//    }

//    private Color GetModeColor(PlayerMode mode) => mode switch
//    {
//        PlayerMode.Warrior => new Color(1f, 0.3f, 0.3f),
//        PlayerMode.Thunder => new Color(0.3f, 0.7f, 1f),
//        _ => Color.gray
//    };

//    private string GetModeText(PlayerMode mode) => mode switch
//    {
//        PlayerMode.Warrior => "闘神",
//        PlayerMode.Thunder => "雷神",
//        _ => "不明"
//    };

//    private string GetChargeText(ChargeLevel level) => level switch
//    {
//        ChargeLevel.None => "なし",
//        ChargeLevel.Level1 => "溜め1",
//        ChargeLevel.Level2 => "溜め2",
//        ChargeLevel.Level3 => "溜め3",
//        _ => "不明"
//    };
//}
