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
    [SerializeField] private int _predictionStartLevel = 1;
    [SerializeField] private float _predictionStartExperience;
    [SerializeField] private float _predictionLevelExperience = 100f;
    [SerializeField] private GameObject _predictionManager;

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

        DrawLevelPredictionSettings();

        _sequenceSO.Update();
        var waves = _sequenceSO.FindProperty("Waves");

        var pendingOp = ListOp.None;
        var pendingIndex = -1;

        double cumulativeExperience = 0;
        bool hasDefaultExperience = false;

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
            waveAsset = element.objectReferenceValue as WaveData;
            if (waveAsset != null)
            {
                DrawSequenceWaveExperience(waveAsset, i);
                bool knownExperience = TryGetWaveExperience(waveAsset, out double waveExperience);
                cumulativeExperience += waveExperience;
                if (!knownExperience)
                    hasDefaultExperience = true;
                string reward = knownExperience ? $"{waveExperience:0.##} EXP" : "算出不可";
                EditorGUILayout.LabelField($"    Wave {i + 1}: {reward} / 累計EXP: {cumulativeExperience:0.##}" +
                    (hasDefaultExperience ? " ＋ 未算出分" : ""), EditorStyles.miniLabel);
                if (!hasDefaultExperience)
                {
                    double experience = _predictionStartExperience + cumulativeExperience;
                    double levelUps = System.Math.Floor(experience / _predictionLevelExperience);
                    double remaining = experience % _predictionLevelExperience;
                    EditorGUILayout.LabelField(
                        $"    完了後の予測PlayerLevel: Lv.{_predictionStartLevel + levelUps:0}  " +
                        $"（次Lvまで {_predictionLevelExperience - remaining:0.##} EXP）", EditorStyles.boldLabel);
                }
                else
                    EditorGUILayout.LabelField("    予測PlayerLevel: 算出不可（未設定・未解決のWaveあり）", EditorStyles.miniLabel);
            }
            else hasDefaultExperience = true;
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

    private void DrawSequenceWaveExperience(WaveData wave, int index)
    {
        bool hasEstimate = TryGetWaveExperience(wave, out double estimatedExperience);
        int displayedExperience = wave.OverrideExperience
            ? wave.TotalExperience
            : hasEstimate ? (int)System.Math.Min(int.MaxValue, System.Math.Round(estimatedExperience)) : 0;

        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();
        bool overrideExperience = EditorGUILayout.ToggleLeft(
            new GUIContent("Wave別に指定", "OFFでは敵別設定を使用。EXP欄に入力すると自動でONになります。"),
            wave.OverrideExperience, GUILayout.Width(110));
        bool modeChanged = EditorGUI.EndChangeCheck();
        EditorGUI.BeginChangeCheck();
        int experience = Mathf.Max(0, EditorGUILayout.IntField(
            new GUIContent($"Wave {index + 1} 合計EXP", "このWaveDataに保存します。同じアセットを参照するWaveには共通で反映されます。"),
            displayedExperience));
        bool amountChanged = EditorGUI.EndChangeCheck();
        EditorGUILayout.EndHorizontal();

        if (!modeChanged && !amountChanged) return;
        var serializedWave = new SerializedObject(wave);
        serializedWave.FindProperty("OverrideExperience").boolValue = amountChanged || overrideExperience;
        serializedWave.FindProperty("TotalExperience").intValue = experience;
        serializedWave.ApplyModifiedProperties();
        Repaint();
    }

    private void DrawLevelPredictionSettings()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("到達レベル予測", EditorStyles.boldLabel);
        if (_predictionManager == null)
            _predictionManager = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/System/Manager.prefab");
        _predictionManager = (GameObject)EditorGUILayout.ObjectField(
            "敵・EXP設定の参照元", _predictionManager, typeof(GameObject), true);
        _predictionStartLevel = Mathf.Max(1, EditorGUILayout.IntField("開始PlayerLevel", _predictionStartLevel));
        _predictionStartExperience = Mathf.Max(0, EditorGUILayout.FloatField("開始Lv内の持ち込みEXP", _predictionStartExperience));
        _predictionLevelExperience = Mathf.Max(0.001f, EditorGUILayout.FloatField("1Lvに必要なEXP", _predictionLevelExperience));
        if (GUILayout.Button("参照元から必要EXPを取得"))
        {
            var manager = _predictionManager != null ? _predictionManager.GetComponentInChildren<EXPManager>(true) : null;
            if (manager != null)
                _predictionLevelExperience = Mathf.Max(0.001f,
                    new SerializedObject(manager).FindProperty("_levelUpEXP").floatValue);
        }
        EditorGUILayout.HelpBox("1周目を順番に全撃破・全オーブ回収した場合の予測です。時間内の到達を保証する値ではありません。敵別設定は参照元のPrefabから算出し、レベル依存の中ボスは合計EXP指定時のみ算出します。", MessageType.Info);
        EditorGUILayout.EndVertical();
    }

    private bool TryGetWaveExperience(WaveData wave, out double experience)
    {
        experience = 0;
        if (wave.OverrideExperience)
        {
            if (wave.GetEnemyCount() > 0) experience = Mathf.Max(0, wave.TotalExperience);
            return true;
        }
        if (_predictionManager == null) return false;
        var spawner = _predictionManager.GetComponentInChildren<EnemySpawner>(true);
        var items = _predictionManager.GetComponentInChildren<EXPItemManager>(true);
        if (spawner == null || items == null) return false;
        var item = new SerializedObject(items).FindProperty("_itemPrefab").objectReferenceValue;
        if (item == null) return false;
        float orbExperience = new SerializedObject(item).FindProperty("_expValue").floatValue;
        var registry = new SerializedObject(spawner).FindProperty("_enemyData");
        double total = 0;
        foreach (var group in wave.SpawnGroups)
        foreach (var entry in group.SpawnEntries)
        {
            if (entry.SpawnCount <= 0) continue;
            if (entry.IsMidBoss) return false;
            Enemy enemy = null;
            for (int i = 0; i < registry.arraySize; i++)
            {
                var candidate = registry.GetArrayElementAtIndex(i);
                if (candidate.FindPropertyRelative("_key").stringValue != entry.EnemyTypeKey) continue;
                var prefab = candidate.FindPropertyRelative("_prefab").objectReferenceValue;
                if (prefab is GameObject obj) enemy = obj.GetComponent<Enemy>();
                else if (prefab is Component component) enemy = component.GetComponent<Enemy>();
                break;
            }
            if (enemy == null) return false;
            var data = new SerializedObject(enemy).FindProperty("_data").objectReferenceValue as EnemyData;
            if (data == null) return false;
            total += (double)entry.SpawnCount * data.ExpDropAmount * orbExperience;
        }
        experience = total;
        return true;
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
        DrawExperienceSettings();
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

    private void DrawExperienceSettings()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("経験値", EditorStyles.boldLabel);
        var enabled = _waveSO.FindProperty("OverrideExperience");
        var total = _waveSO.FindProperty("TotalExperience");
        EditorGUILayout.PropertyField(enabled, new GUIContent("Wave合計EXPを指定"));
        using (new EditorGUI.DisabledScope(!enabled.boolValue))
        {
            EditorGUILayout.PropertyField(total, new GUIContent("合計EXP"));
            total.intValue = Mathf.Max(0, total.intValue);
        }
        if (enabled.boolValue)
        {
            int count = _wave.GetEnemyCount();
            string distribution = count > 0
                ? $"敵 {count} 体に配分ウェイトで分配します（1体あたりの比率）。"
                : "敵が設定されていないため、経験値はドロップしません。";
            EditorGUILayout.HelpBox(distribution + "\n全撃破・全オーブ回収時の合計です。途中終了では倒した敵の分だけ獲得します。", MessageType.Info);
            int enemyIndex = 0;
            int groupIndex = 0;
            foreach (var group in _wave.SpawnGroups)
            {
                groupIndex++;
                foreach (var entry in group.SpawnEntries)
                {
                    int entryExperience = 0;
                    int min = int.MaxValue;
                    int max = 0;
                    for (int i = 0; i < entry.SpawnCount; i++)
                    {
                        int amount = _wave.GetExperienceForEnemy(enemyIndex++) ?? 0;
                        entryExperience += amount;
                        min = Mathf.Min(min, amount);
                        max = Mathf.Max(max, amount);
                    }
                    if (entry.SpawnCount <= 0) continue;
                    string perEnemy = min == max ? $"{min}" : $"{min}〜{max}";
                    EditorGUILayout.LabelField($"G{groupIndex} {entry.EnemyTypeKey} ×{entry.SpawnCount}",
                        $"計 {entryExperience} EXP / 1体 {perEnemy}");
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("敵のドロップ数とオーブの経験値設定を使用します。", MessageType.Info);
        }
        EditorGUILayout.EndVertical();
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
            var experienceWeight = entry.FindPropertyRelative("ExperienceWeight");
            EditorGUILayout.PropertyField(experienceWeight, new GUIContent("EXP配分ウェイト（1体）"));
            experienceWeight.intValue = Mathf.Max(1, experienceWeight.intValue);
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
