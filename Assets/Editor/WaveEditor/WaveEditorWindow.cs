#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// WaveSequenceData / WaveDataを階層ごとに編集できる拡張エディター
/// </summary>
public class WaveEditorWindow : EditorWindow
{
    [MenuItem("Tools/Wave/Wave Editor")]
    private static void OpenFromMenu()
    {
        var window = GetWindow<WaveEditorWindow>("Wave Editor");
        window.minSize = new Vector2(480f, 560f);
        window.Show();
    }

    private WaveSequenceData _sequence;
    private SerializedObject _sequenceSO;
    private WaveData _wave;
    private SerializedObject _waveSO;
    private Vector2 _scroll;

    private readonly Dictionary<string, bool> _foldouts = new();
    private static List<string> _enemyKeyCache = new();

    private void OnEnable()
    {
        RefreshEnemyKeyCache();
        Selection.selectionChanged += HandleSelectionChanged;
    }

    private void OnDisable()
    {
        Selection.selectionChanged -= HandleSelectionChanged;
    }

    private void OnGUI()
    {
        DrawToolbar();

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        DrawSequenceSection();
        EditorGUILayout.Space(8);
        DrawWaveSection();
        EditorGUILayout.EndScrollView();
    }

    /// <summary> Project選択がSequence/Waveならウィンドウに自動反映する </summary>
    private void HandleSelectionChanged()
    {
        switch (Selection.activeObject)
        {
            case WaveSequenceData sequence:
                SetSequence(sequence);
                Repaint();
                break;
            case WaveData wave:
                SetWave(wave);
                Repaint();
                break;
        }
    }

    /// <summary> キー候補の再収集ボタンを描画する </summary>
    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        if (GUILayout.Button("敵キー候補を更新", EditorStyles.toolbarButton, GUILayout.Width(120)))
            RefreshEnemyKeyCache();
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    /// <summary> WaveSequenceDataが持つWave一覧を描画する </summary>
    private void DrawSequenceSection()
    {
        EditorGUILayout.LabelField("Wave Sequence", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        var sequence = (WaveSequenceData)EditorGUILayout.ObjectField(
            "Sequence", _sequence, typeof(WaveSequenceData), false);
        if (EditorGUI.EndChangeCheck())
            SetSequence(sequence);

        if (_sequence == null || _sequenceSO == null) return;

        _sequenceSO.Update();
        var waves = _sequenceSO.FindProperty("Waves");

        var pendingOp = ListOp.None;
        var pendingIndex = -1;

        for (int i = 0; i < waves.arraySize; i++)
        {
            var element = waves.GetArrayElementAtIndex(i);
            var waveAsset = element.objectReferenceValue as WaveData;

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"[{i}]", GUILayout.Width(28));
            EditorGUILayout.PropertyField(element, GUIContent.none);

            EditorGUI.BeginDisabledGroup(waveAsset == null);
            if (GUILayout.Button("編集", GUILayout.Width(40)))
                SetWave(waveAsset);
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(i == 0);
            if (GUILayout.Button("↑", GUILayout.Width(24))) { pendingOp = ListOp.MoveUp; pendingIndex = i; }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(i == waves.arraySize - 1);
            if (GUILayout.Button("↓", GUILayout.Width(24))) { pendingOp = ListOp.MoveDown; pendingIndex = i; }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(waveAsset == null);
            if (GUILayout.Button("複製", GUILayout.Width(40))) { pendingOp = ListOp.Duplicate; pendingIndex = i; }
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("削除", GUILayout.Width(40))) { pendingOp = ListOp.Delete; pendingIndex = i; }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("空スロットを追加", GUILayout.Width(120)))
            waves.arraySize++;
        if (GUILayout.Button("新規Waveを作成して追加", GUILayout.Width(160)))
        {
            var created = CreateWaveAsset(_sequence.name);
            waves.arraySize++;
            waves.GetArrayElementAtIndex(waves.arraySize - 1).objectReferenceValue = created;
        }
        EditorGUILayout.EndHorizontal();

        if (pendingOp != ListOp.None)
            ApplySequenceListOp(waves, pendingIndex, pendingOp);

        _sequenceSO.ApplyModifiedProperties();
    }

    /// <summary> Wave一覧に対するMove/Duplicate/Deleteを1件適用する </summary>
    private void ApplySequenceListOp(SerializedProperty waves, int index, ListOp op)
    {
        switch (op)
        {
            case ListOp.MoveUp:
                waves.MoveArrayElement(index, index - 1);
                break;
            case ListOp.MoveDown:
                waves.MoveArrayElement(index, index + 1);
                break;
            case ListOp.Delete:
                DeleteArrayElement(waves, index);
                break;
            case ListOp.Duplicate:
                var original = waves.GetArrayElementAtIndex(index).objectReferenceValue as WaveData;
                var duplicated = DuplicateWaveAsset(original);
                waves.InsertArrayElementAtIndex(index);
                waves.GetArrayElementAtIndex(index + 1).objectReferenceValue = duplicated;
                break;
        }
    }

    /// <summary> 選択中WaveDataのSpawnGroup一覧を描画する </summary>
    private void DrawWaveSection()
    {
        EditorGUILayout.LabelField("Wave Data", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        var wave = (WaveData)EditorGUILayout.ObjectField("Wave", _wave, typeof(WaveData), false);
        if (EditorGUI.EndChangeCheck())
            SetWave(wave);

        if (_wave == null || _waveSO == null)
        {
            EditorGUILayout.HelpBox("編集するWaveDataを選択してください。", MessageType.Info);
            return;
        }

        _waveSO.Update();
        var groups = _waveSO.FindProperty("SpawnGroups");

        var pendingOp = ListOp.None;
        var pendingIndex = -1;

        for (int i = 0; i < groups.arraySize; i++)
        {
            DrawSpawnGroupElement(groups.GetArrayElementAtIndex(i), i, groups.arraySize, out var op);
            if (op == ListOp.None) continue;
            pendingOp = op;
            pendingIndex = i;
        }

        if (GUILayout.Button("SpawnGroupを追加", GUILayout.Width(140)))
            groups.arraySize++;

        if (pendingOp != ListOp.None)
            ApplyGenericListOp(groups, pendingIndex, pendingOp);

        _waveSO.ApplyModifiedProperties();
    }

    /// <summary> 1つのSpawnGroupのヘッダーと中身を描画する </summary>
    private void DrawSpawnGroupElement(SerializedProperty group, int index, int count, out ListOp op)
    {
        op = ListOp.None;

        var entries = group.FindPropertyRelative("SpawnEntries");
        var placementMode = group.FindPropertyRelative("_placementMode");
        var foldoutKey = group.propertyPath;
        if (!_foldouts.TryGetValue(foldoutKey, out var expanded))
            expanded = true;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();

        var mode = (SpawnGroupData.SpawnPlacementMode)placementMode.enumValueIndex;
        expanded = EditorGUILayout.Foldout(expanded, $"SpawnGroup [{index}]  敵{entries.arraySize}種  ({mode})", true);
        GUILayout.FlexibleSpace();

        EditorGUI.BeginDisabledGroup(index == 0);
        if (GUILayout.Button("↑", GUILayout.Width(24))) op = ListOp.MoveUp;
        EditorGUI.EndDisabledGroup();

        EditorGUI.BeginDisabledGroup(index == count - 1);
        if (GUILayout.Button("↓", GUILayout.Width(24))) op = ListOp.MoveDown;
        EditorGUI.EndDisabledGroup();

        if (GUILayout.Button("複製", GUILayout.Width(40))) op = ListOp.Duplicate;
        if (GUILayout.Button("削除", GUILayout.Width(40))) op = ListOp.Delete;
        EditorGUILayout.EndHorizontal();

        _foldouts[foldoutKey] = expanded;

        if (expanded)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField("SpawnEntries", EditorStyles.miniBoldLabel);
            DrawSpawnEntriesList(entries);

            EditorGUILayout.Space(2);
            EditorGUILayout.PropertyField(group.FindPropertyRelative("ExclusionRadius"));
            EditorGUILayout.PropertyField(group.FindPropertyRelative("SpawnPointKey"));
            EditorGUILayout.PropertyField(group.FindPropertyRelative("_spawnSetSize"), new GUIContent("SpawnSetSize"));
            EditorGUILayout.PropertyField(group.FindPropertyRelative("_spawnSetInterval"), new GUIContent("SpawnSetInterval"));
            EditorGUILayout.PropertyField(placementMode, new GUIContent("PlacementMode"));

            // Clusterモードの時だけ半径設定を出す
            if (mode == SpawnGroupData.SpawnPlacementMode.Cluster)
                EditorGUILayout.PropertyField(group.FindPropertyRelative("_clusterRadius"), new GUIContent("ClusterRadius"));

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("NextWaveConditions", EditorStyles.miniBoldLabel);
            DrawNextConditionsList(group.FindPropertyRelative("NextWaveConditions"));

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary> SpawnGroup内の出現エネミー一覧を描画する </summary>
    private void DrawSpawnEntriesList(SerializedProperty entries)
    {
        var pendingOp = ListOp.None;
        var pendingIndex = -1;

        for (int i = 0; i < entries.arraySize; i++)
        {
            var entry = entries.GetArrayElementAtIndex(i);

            EditorGUILayout.BeginHorizontal();
            DrawEnemyTypeKeyField(entry.FindPropertyRelative("EnemyTypeKey"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("SpawnCount"), GUIContent.none, GUILayout.Width(50));

            EditorGUI.BeginDisabledGroup(i == 0);
            if (GUILayout.Button("↑", GUILayout.Width(24))) { pendingOp = ListOp.MoveUp; pendingIndex = i; }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(i == entries.arraySize - 1);
            if (GUILayout.Button("↓", GUILayout.Width(24))) { pendingOp = ListOp.MoveDown; pendingIndex = i; }
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("複製", GUILayout.Width(40))) { pendingOp = ListOp.Duplicate; pendingIndex = i; }
            if (GUILayout.Button("削除", GUILayout.Width(40))) { pendingOp = ListOp.Delete; pendingIndex = i; }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(entry.FindPropertyRelative("MidBossLevelTable"), new GUIContent("MidBoss Table"));
        }

        if (GUILayout.Button("敵を追加", GUILayout.Width(80)))
            entries.arraySize++;

        if (pendingOp != ListOp.None)
            ApplyGenericListOp(entries, pendingIndex, pendingOp);
    }

    /// <summary> 次グループへ進む条件一覧を描画する </summary>
    private void DrawNextConditionsList(SerializedProperty conditions)
    {
        var pendingOp = ListOp.None;
        var pendingIndex = -1;

        for (int i = 0; i < conditions.arraySize; i++)
        {
            var condition = conditions.GetArrayElementAtIndex(i);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(condition.FindPropertyRelative("WaveConditionType"), GUIContent.none, GUILayout.Width(120));
            EditorGUILayout.PropertyField(condition.FindPropertyRelative("Threshold"), GUIContent.none, GUILayout.Width(60));

            if (GUILayout.Button("複製", GUILayout.Width(40))) { pendingOp = ListOp.Duplicate; pendingIndex = i; }
            if (GUILayout.Button("削除", GUILayout.Width(40))) { pendingOp = ListOp.Delete; pendingIndex = i; }
            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button("条件を追加", GUILayout.Width(80)))
            conditions.arraySize++;

        if (pendingOp != ListOp.None)
            ApplyGenericListOp(conditions, pendingIndex, pendingOp);
    }

    /// <summary> EnemyTypeKey入力欄と候補選択ボタンを描画する </summary>
    private void DrawEnemyTypeKeyField(SerializedProperty keyProp)
    {
        var isEmpty = string.IsNullOrEmpty(keyProp.stringValue);
        var prevColor = GUI.color;
        if (isEmpty) GUI.color = Color.yellow;
        EditorGUILayout.PropertyField(keyProp, GUIContent.none, GUILayout.MinWidth(80));
        GUI.color = prevColor;

        if (GUILayout.Button("▼", GUILayout.Width(20)))
            ShowEnemyKeyMenu(keyProp.serializedObject, keyProp.propertyPath, keyProp.stringValue);
    }

    /// <summary> 既存アセットから収集したEnemyTypeKey候補メニューを表示する </summary>
    private void ShowEnemyKeyMenu(SerializedObject owner, string propertyPath, string currentValue)
    {
        var menu = new GenericMenu();
        if (_enemyKeyCache.Count == 0)
        {
            menu.AddDisabledItem(new GUIContent("候補がありません"));
        }
        else
        {
            foreach (var key in _enemyKeyCache)
            {
                var captured = key;
                menu.AddItem(new GUIContent(captured), captured == currentValue,
                    () => ApplyEnemyKey(owner, propertyPath, captured));
            }
        }
        menu.ShowAsContext();
    }

    /// <summary> propertyPathからプロパティを再解決してEnemyTypeKeyを書き込む </summary>
    private void ApplyEnemyKey(SerializedObject owner, string propertyPath, string value)
    {
        var prop = owner.FindProperty(propertyPath);
        if (prop == null) return;
        prop.stringValue = value;
        owner.ApplyModifiedProperties();
        Repaint();
    }

    private void SetSequence(WaveSequenceData sequence)
    {
        _sequence = sequence;
        _sequenceSO = sequence != null ? new SerializedObject(sequence) : null;
    }

    private void SetWave(WaveData wave)
    {
        _wave = wave;
        _waveSO = wave != null ? new SerializedObject(wave) : null;
    }

    /// <summary> Sequenceと同じフォルダに空のWaveDataアセットを新規作成する </summary>
    private WaveData CreateWaveAsset(string context)
    {
        var folder = _sequence != null
            ? Path.GetDirectoryName(AssetDatabase.GetAssetPath(_sequence))
            : "Assets/Data/Wave";
        if (string.IsNullOrEmpty(folder) || !AssetDatabase.IsValidFolder(folder))
            folder = "Assets";

        var path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/WaveData_{context}.asset");
        var asset = CreateInstance<WaveData>();
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        RefreshEnemyKeyCache();
        return asset;
    }

    /// <summary> 既存WaveDataアセットを別ファイルとして複製する </summary>
    private WaveData DuplicateWaveAsset(WaveData original)
    {
        if (original == null) return null;

        var srcPath = AssetDatabase.GetAssetPath(original);
        var dstPath = AssetDatabase.GenerateUniqueAssetPath(srcPath);
        AssetDatabase.CopyAsset(srcPath, dstPath);
        AssetDatabase.SaveAssets();
        RefreshEnemyKeyCache();
        return AssetDatabase.LoadAssetAtPath<WaveData>(dstPath);
    }

    /// <summary> Move/Duplicate/Deleteを配列プロパティに1件適用する </summary>
    private static void ApplyGenericListOp(SerializedProperty array, int index, ListOp op)
    {
        switch (op)
        {
            case ListOp.MoveUp:
                array.MoveArrayElement(index, index - 1);
                break;
            case ListOp.MoveDown:
                array.MoveArrayElement(index, index + 1);
                break;
            case ListOp.Duplicate:
                array.InsertArrayElementAtIndex(index);
                break;
            case ListOp.Delete:
                DeleteArrayElement(array, index);
                break;
        }
    }

    /// <summary> ObjectReference要素はnull化してから消す（Unityの削除仕様への対応） </summary>
    private static void DeleteArrayElement(SerializedProperty array, int index)
    {
        var element = array.GetArrayElementAtIndex(index);
        if (element.propertyType == SerializedPropertyType.ObjectReference && element.objectReferenceValue != null)
            element.objectReferenceValue = null;
        array.DeleteArrayElementAtIndex(index);
    }

    /// <summary> プロジェクト内の全WaveDataからEnemyTypeKeyの使用済み一覧を収集する </summary>
    private static void RefreshEnemyKeyCache()
    {
        var keys = new HashSet<string>();
        foreach (var guid in AssetDatabase.FindAssets("t:WaveData"))
        {
            var wave = AssetDatabase.LoadAssetAtPath<WaveData>(AssetDatabase.GUIDToAssetPath(guid));
            if (wave == null) continue;

            foreach (var group in wave.SpawnGroups)
            {
                foreach (var entry in group.SpawnEntries)
                {
                    if (!string.IsNullOrEmpty(entry.EnemyTypeKey))
                        keys.Add(entry.EnemyTypeKey);
                }
            }
        }
        _enemyKeyCache = keys.OrderBy(k => k).ToList();
    }

    private enum ListOp
    {
        None,
        MoveUp,
        MoveDown,
        Duplicate,
        Delete,
    }
}
#endif
