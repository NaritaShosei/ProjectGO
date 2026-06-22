using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BossEnemy.Data;
using UnityEditor;
using UnityEngine;

public class BossEnemyMasterDataCsvImporter : EditorWindow
{
    private const string DefaultCsvPath = "Assets/Data/CSV/BossEnemyMasterDataCSV.csv";
    private const string DefaultOutputFolder = "Assets/Data/BossEnemy/MasterData";

    [SerializeField] private TextAsset _csvAsset;
    [SerializeField] private int _bossId = 1;
    [SerializeField] private string _outputFolder = DefaultOutputFolder;
    [SerializeField] private bool _overwriteExisting = true;

    [MenuItem("Tools/BossEnemy/Create MasterData From CSV")]
    private static void Open()
    {
        BossEnemyMasterDataCsvImporter window = GetWindow<BossEnemyMasterDataCsvImporter>("Boss CSV Importer");
        window.minSize = new Vector2(420f, 180f);
        window.Show();
    }

    private void OnEnable()
    {
        if (_csvAsset == null)
        {
            _csvAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(DefaultCsvPath);
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("BossEnemyMasterData CSV Importer", EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);

        _csvAsset = (TextAsset)EditorGUILayout.ObjectField("CSV", _csvAsset, typeof(TextAsset), false);
        _bossId = EditorGUILayout.IntField("Boss ID", _bossId);
        _outputFolder = EditorGUILayout.TextField("Output Folder", _outputFolder);
        _overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing", _overwriteExisting);

        EditorGUILayout.Space(8f);

        using (new EditorGUI.DisabledScope(_csvAsset == null))
        {
            if (GUILayout.Button("Create BossEnemyMasterData", GUILayout.Height(32f)))
            {
                CreateMasterData();
            }
        }
    }

    private void CreateMasterData()
    {
        try
        {
            if (_csvAsset == null)
            {
                EditorUtility.DisplayDialog("CSV Import Error", "Please select a CSV file.", "OK");
                return;
            }

            BossEnemyMasterData masterData = BuildMasterData(_csvAsset.text, _bossId);
            string assetName = SanitizeFileName(masterData.BossName);
            string assetPath = GetAssetPath(_outputFolder, $"ID{_bossId}_BossEnemy.asset");

            EnsureAssetFolder(_outputFolder);

            if (_overwriteExisting && AssetDatabase.LoadAssetAtPath<BossEnemyMasterData>(assetPath) != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
            else
            {
                assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
            }

            AssetDatabase.CreateAsset(masterData, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = masterData;

            EditorUtility.DisplayDialog(
                "CSV Import Complete",
                $"Created BossEnemyMasterData.\n\n{assetPath}",
                "OK");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorUtility.DisplayDialog("CSV Import Error", ex.Message, "OK");
        }
    }

    private static BossEnemyMasterData BuildMasterData(string csvText, int bossId)
    {
        List<string[]> rows = ParseCsv(csvText);
        int bossRowIndex = FindBossRow(rows, bossId);
        if (bossRowIndex < 0)
        {
            throw new InvalidOperationException($"BossEnemyData ID {bossId} was not found in the CSV.");
        }

        string bossName = GetCell(rows[bossRowIndex], 1);
        if (string.IsNullOrWhiteSpace(bossName))
        {
            bossName = $"BossEnemy_{bossId}";
        }

        List<BossEnemyData> phaseDataList = new List<BossEnemyData>();
        int index = bossRowIndex + 1;

        while (index < rows.Count && !IsAttackDataSection(rows[index]) && !IsBossHeaderRow(rows[index]))
        {
            string[] statusRow = rows[index];
            if (!IsPhaseStatusRow(statusRow))
            {
                index++;
                continue;
            }

            if (index + 8 >= rows.Count)
            {
                throw new InvalidOperationException($"Phase data after row {index + 1} is incomplete.");
            }

            BossEnemyData phaseData = CreatePhaseData(rows, index);
            phaseDataList.Add(phaseData);
            index += 9;
        }

        if (phaseDataList.Count == 0)
        {
            throw new InvalidOperationException($"Phase data for BossEnemyData ID {bossId} was not found.");
        }

        BossEnemyMasterData masterData = CreateInstance<BossEnemyMasterData>();
        masterData.DataConstruct(phaseDataList.ToArray(), bossName);
        return masterData;
    }

    private static BossEnemyData CreatePhaseData(List<string[]> rows, int statusRowIndex)
    {
        string[] statusRow = rows[statusRowIndex];
        string[] attackIdsRow = rows[statusRowIndex + 5];
        string[] closeRangeRateRow = rows[statusRowIndex + 6];
        string[] finishCountRateRow = rows[statusRowIndex + 7];
        string[] longRangeRateRow = rows[statusRowIndex + 8];

        BossEnemyData phaseData = new BossEnemyData();
        phaseData.DataConstruct(
            maxHP: ParseRequiredInt(statusRow, 2, statusRowIndex, "MaxHP"),
            hardSpotsDefense: ParseRequiredInt(statusRow, 6, statusRowIndex, "HardSpotsDefense"),
            normalSpotsDefense: ParseRequiredInt(statusRow, 5, statusRowIndex, "NormalSpotsDefense"),
            weekPointDefense: ParseRequiredInt(statusRow, 4, statusRowIndex, "WeekPointDefense"),
            vitalPointDefense: ParseRequiredInt(statusRow, 3, statusRowIndex, "VitalPointDefense"),
            rightArmArmer: CreateArmorData(rows, statusRowIndex + 1, "RightArmArmor"),
            leftArmArmer: CreateArmorData(rows, statusRowIndex + 2, "LeftArmArmor"),
            rightLegArmer: CreateArmorData(rows, statusRowIndex + 3, "RightLegArmor"),
            leftLegArmer: CreateArmorData(rows, statusRowIndex + 4, "LeftLegArmor"),
            closeRangeNormalAttackDataHolder: CreateAttackField(attackIdsRow, closeRangeRateRow),
            closeRangeFinishCountAttackDataHolder: CreateAttackField(attackIdsRow, finishCountRateRow),
            longRangeAttackDataHolder: CreateAttackField(attackIdsRow, longRangeRateRow));

        return phaseData;
    }

    private static BossArmorData CreateArmorData(List<string[]> rows, int rowIndex, string expectedPartName)
    {
        string[] row = rows[rowIndex];
        string partName = GetCell(row, 1);
        if (!string.Equals(partName, expectedPartName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"CSV row {rowIndex + 1} must be {expectedPartName}. Current: {partName}");
        }

        BossArmorData armorData = new BossArmorData();
        armorData.DataConstruct(
            ParseRequiredInt(row, 2, rowIndex, $"{expectedPartName} HP"),
            ParseRequiredInt(row, 3, rowIndex, $"{expectedPartName} Defense"));
        return armorData;
    }

    private static BossEnemyAttackField CreateAttackField(string[] attackIdsRow, string[] activationRatesRow)
    {
        List<BossEnemyAttackField.AttackCondition> conditions = new List<BossEnemyAttackField.AttackCondition>();

        for (int column = 2; column < attackIdsRow.Length; column++)
        {
            string idText = GetCell(attackIdsRow, column);
            if (string.IsNullOrWhiteSpace(idText))
            {
                continue;
            }

            BossEnemyAttackField.AttackCondition condition = new BossEnemyAttackField.AttackCondition
            {
                ID = ParseRequiredInt(attackIdsRow, column, -1, "Attack ID"),
                ActivationRate = ParseRequiredInt(activationRatesRow, column, -1, "Activation Rate")
            };
            conditions.Add(condition);
        }

        BossEnemyAttackField attackField = new BossEnemyAttackField();
        attackField.DataConstruct(conditions.ToArray());
        return attackField;
    }

    private static int FindBossRow(List<string[]> rows, int bossId)
    {
        for (int i = 0; i < rows.Count; i++)
        {
            if (IsAttackDataSection(rows[i]))
            {
                break;
            }

            if (IsBossHeaderRow(rows[i]) && ParseOptionalInt(rows[i], 0) == bossId)
            {
                return i;
            }
        }

        return -1;
    }

    private static bool IsBossHeaderRow(string[] row)
    {
        return ParseOptionalInt(row, 0).HasValue && !string.IsNullOrWhiteSpace(GetCell(row, 1));
    }

    private static bool IsPhaseStatusRow(string[] row)
    {
        return string.IsNullOrWhiteSpace(GetCell(row, 0)) && ParseOptionalInt(row, 1).HasValue;
    }

    private static bool IsAttackDataSection(string[] row)
    {
        return string.Equals(GetCell(row, 0), "AttackData", StringComparison.OrdinalIgnoreCase);
    }

    private static int ParseRequiredInt(string[] row, int column, int rowIndex, string label)
    {
        string value = GetCell(row, column);
        if (int.TryParse(value, out int result))
        {
            return result;
        }

        string rowLabel = rowIndex >= 0 ? $"row {rowIndex + 1}" : "CSV";
        throw new FormatException($"{rowLabel} {label} is not a valid int. Value: {value}");
    }

    private static int? ParseOptionalInt(string[] row, int column)
    {
        if (int.TryParse(GetCell(row, column), out int result))
        {
            return result;
        }

        return null;
    }

    private static string GetCell(string[] row, int index)
    {
        return index >= 0 && index < row.Length ? row[index].Trim() : string.Empty;
    }

    private static List<string[]> ParseCsv(string text)
    {
        List<string[]> rows = new List<string[]>();
        List<string> row = new List<string>();
        StringBuilder cell = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < text.Length && text[i + 1] == '"')
                {
                    cell.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                row.Add(cell.ToString());
                cell.Length = 0;
            }
            else if ((c == '\n' || c == '\r') && !inQuotes)
            {
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                }

                row.Add(cell.ToString());
                cell.Length = 0;
                rows.Add(row.ToArray());
                row.Clear();
            }
            else
            {
                cell.Append(c);
            }
        }

        if (cell.Length > 0 || row.Count > 0)
        {
            row.Add(cell.ToString());
            rows.Add(row.ToArray());
        }

        return rows;
    }

    private static void EnsureAssetFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string[] parts = folderPath.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    private static string GetAssetPath(string folder, string fileName)
    {
        return $"{folder.TrimEnd('/')}/{fileName}";
    }

    private static string SanitizeFileName(string fileName)
    {
        foreach (char invalidChar in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(invalidChar, '_');
        }

        return string.IsNullOrWhiteSpace(fileName) ? "BossEnemyMasterData" : fileName;
    }
}
