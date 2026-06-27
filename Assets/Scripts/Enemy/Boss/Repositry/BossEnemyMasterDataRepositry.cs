using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using BossEnemy.Data;

namespace BossEnemy.Data.Repository
{
    public class BossEnemyMasterDataRepository
    {
        // ボスのマスターデータをキャッシュする辞書 (Key: ボスID, Value: 生成されたMasterData)
        private readonly Dictionary<int, BossEnemyMasterData> _masterDataCache = new Dictionary<int, BossEnemyMasterData>();

        // リフレクション用リファレンス（実機ビルドでDataConstructが消えていても、非パブリックフィールドへ直接注入可能にする）
        private static readonly FieldInfo MasterDatasField = typeof(BossEnemyMasterData).GetField("_bossEnemyDatas", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo BossNameField = typeof(BossEnemyMasterData).GetField("_bossName", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo TotalPhaseCountField = typeof(BossEnemyMasterData).GetField("_totalPhaseCount", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly FieldInfo MaxHPField = typeof(BossEnemyData).GetField("_maxHP", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo WalkSpeedField = typeof(BossEnemyData).GetField("_walkSpeed", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo HardSpotsDefenseField = typeof(BossEnemyData).GetField("_hardSpotsDefense", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo NormalSpotsDefenseField = typeof(BossEnemyData).GetField("_normalSpotsDefense", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo WeekPointDefenseField = typeof(BossEnemyData).GetField("_weekPointDefense", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo VitalPointDefenseField = typeof(BossEnemyData).GetField("_vitalPointDefense", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo RightArmArmorField = typeof(BossEnemyData).GetField("_rightArmArmer", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo LeftArmArmorField = typeof(BossEnemyData).GetField("_leftArmArmer", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo RightLegArmorField = typeof(BossEnemyData).GetField("_rightLegArmer", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo LeftLegArmorField = typeof(BossEnemyData).GetField("_leftLegArmer", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo CloseRangeNormalField = typeof(BossEnemyData).GetField("_closeRangeNormalAttackDataHolder", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo CloseRangeFinishField = typeof(BossEnemyData).GetField("_closeRangeFinishCountAttackDataHolder", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo LongRangeField = typeof(BossEnemyData).GetField("_longRangeAttackDataHolder", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly FieldInfo ArmorMaxHPField = typeof(BossArmorData).GetField("_maxHP", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo ArmorDefenseField = typeof(BossArmorData).GetField("_defense", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly FieldInfo AttackFieldField = typeof(BossEnemyAttackField).GetField("_attackField", BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        /// CSVのテキストデータをもとにリポジトリを初期化し、すべてのボスデータをメモリにキャッシュします。
        /// </summary>
        public void Init(string csvText)
        {
            _masterDataCache.Clear();

            if (string.IsNullOrEmpty(csvText))
            {
                Debug.LogError("[Repository] CSV text is null or empty.");
                return;
            }

            try
            {
                List<string[]> rows = ParseCsv(csvText);
                ParseAndCacheBossData(rows);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Repository] Failed to initialize repository: {ex.Message}");
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// 指定されたボスIDに対応する BossEnemyMasterData クラスのインスタンスを取得します。
        /// </summary>
        public BossEnemyMasterData GetData(int id)
        {
            if (!_masterDataCache.TryGetValue(id, out var masterData))
            {
                Debug.LogError($"[Repository] Boss ID {id} was not found in the repository.");
                return null;
            }
            return masterData;
        }

        #region CSV Parser Logic

        private void ParseAndCacheBossData(List<string[]> rows)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                // AttackData セクションに入ったらボスデータは終了
                if (string.Equals(GetCell(rows[i], 0), "AttackData", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                // ボスIDヘッダー行を検出
                if (int.TryParse(GetCell(rows[i], 0), out int bossId) && !string.IsNullOrWhiteSpace(GetCell(rows[i], 1)))
                {
                    string bossName = GetCell(rows[i], 1);
                    List<BossEnemyData> phaseList = new List<BossEnemyData>();

                    int index = i + 1;

                    while (index < rows.Count)
                    {
                        string[] currentRow = rows[index];
                        string firstCell = GetCell(currentRow, 0);

                        if (firstCell == "END" || firstCell == "AttackData" || int.TryParse(firstCell, out _))
                        {
                            break;
                        }

                        // 2列目（Phase番号）が数値であればフェーズブロックの開始
                        if (int.TryParse(GetCell(currentRow, 1), out int _))
                        {
                            if (index + 8 >= rows.Count)
                            {
                                throw new InvalidOperationException($"行 {index + 1}: ボスID {bossId} のフェーズブロックが9行に満ちていません。");
                            }

                            BossEnemyData phaseData = CreatePhaseData(rows, index);
                            phaseList.Add(phaseData);
                            index += 9;
                            continue;
                        }

                        index++;
                    }

                    if (phaseList.Count > 0)
                    {
                        // リフレクションによる安全なフィールドインジェクション（IL2CPP実機動作対応）
                        BossEnemyMasterData masterData = new BossEnemyMasterData();
                        MasterDatasField.SetValue(masterData, phaseList.ToArray());
                        BossNameField.SetValue(masterData, bossName);
                        TotalPhaseCountField.SetValue(masterData, phaseList.Count);

                        _masterDataCache[bossId] = masterData;
                    }

                    i = index - 1;
                }
            }
        }

        private static BossEnemyData CreatePhaseData(List<string[]> rows, int statusRowIndex)
        {
            string[] statusRow = rows[statusRowIndex];
            string[] rArmRow = rows[statusRowIndex + 1];
            string[] lArmRow = rows[statusRowIndex + 2];
            string[] rLegRow = rows[statusRowIndex + 3];
            string[] lLegRow = rows[statusRowIndex + 4];

            string[] attackIdsRow = rows[statusRowIndex + 5];
            string[] closeRangeRateRow = rows[statusRowIndex + 6];
            string[] finishCountRateRow = rows[statusRowIndex + 7];
            string[] longRangeRateRow = rows[statusRowIndex + 8];

            BossEnemyData phaseData = new BossEnemyData();

            MaxHPField.SetValue(phaseData, ParseInt(statusRow, 2, statusRowIndex, "最大HP"));
            WalkSpeedField.SetValue(phaseData, ParseFloat(statusRow, 7, statusRowIndex, "移動速度"));
            HardSpotsDefenseField.SetValue(phaseData, ParseInt(statusRow, 6, statusRowIndex, "硬い肉質"));
            NormalSpotsDefenseField.SetValue(phaseData, ParseInt(statusRow, 5, statusRowIndex, "普通の肉質"));
            WeekPointDefenseField.SetValue(phaseData, ParseInt(statusRow, 4, statusRowIndex, "弱点の肉質"));
            VitalPointDefenseField.SetValue(phaseData, ParseInt(statusRow, 3, statusRowIndex, "急所の肉質"));

            RightArmArmorField.SetValue(phaseData, CreateArmorData(rArmRow, statusRowIndex + 1, "RightArmArmor"));
            LeftArmArmorField.SetValue(phaseData, CreateArmorData(lArmRow, statusRowIndex + 2, "LeftArmArmor"));
            RightLegArmorField.SetValue(phaseData, CreateArmorData(rLegRow, statusRowIndex + 3, "RightLegArmor"));
            LeftLegArmorField.SetValue(phaseData, CreateArmorData(lLegRow, statusRowIndex + 4, "LeftLegArmor"));

            CloseRangeNormalField.SetValue(phaseData, CreateAttackField(attackIdsRow, closeRangeRateRow));
            CloseRangeFinishField.SetValue(phaseData, CreateAttackField(attackIdsRow, finishCountRateRow));
            LongRangeField.SetValue(phaseData, CreateAttackField(attackIdsRow, longRangeRateRow));

            return phaseData;
        }

        private static BossArmorData CreateArmorData(string[] row, int rowIndex, string expectedPart)
        {
            string partName = GetCell(row, 1);
            if (!string.Equals(partName, expectedPart, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"行 {rowIndex + 1}: 部位構造ミスマッチ。期待値: '{expectedPart}', 実際: '{partName}'");
            }

            BossArmorData armorData = new BossArmorData();
            ArmorMaxHPField.SetValue(armorData, ParseInt(row, 2, rowIndex, $"{expectedPart} HP"));
            ArmorDefenseField.SetValue(armorData, ParseInt(row, 3, rowIndex, $"{expectedPart} 肉質"));
            return armorData;
        }

        private static BossEnemyAttackField CreateAttackField(string[] idsRow, string[] ratesRow)
        {
            List<BossEnemyAttackField.AttackCondition> conditions = new List<BossEnemyAttackField.AttackCondition>();

            for (int col = 2; col < idsRow.Length; col++)
            {
                string idText = GetCell(idsRow, col);
                if (string.IsNullOrWhiteSpace(idText)) continue;

                BossEnemyAttackField.AttackCondition condition = new BossEnemyAttackField.AttackCondition
                {
                    ID = ParseInt(idsRow, col, -1, "Attack ID"),
                    ActivationRate = ParseInt(ratesRow, col, -1, "Activation Rate")
                };
                conditions.Add(condition);
            }

            BossEnemyAttackField attackField = new BossEnemyAttackField();
            AttackFieldField.SetValue(attackField, conditions.ToArray());
            return attackField;
        }

        #endregion

        #region Utilities

        private static int ParseInt(string[] row, int col, int rIndex, string label)
        {
            string val = GetCell(row, col);
            if (int.TryParse(val, out int res)) return res;
            throw new FormatException($"行 {rIndex + 1}, 列 {col + 1} [{label}]: '{val}' は有効な整数(int)ではありません。");
        }

        private static float ParseFloat(string[] row, int col, int rIndex, string label)
        {
            string val = GetCell(row, col);
            if (float.TryParse(val, out float res)) return res;
            throw new FormatException($"行 {rIndex + 1}, 列 {col + 1} [{label}]: '{val}' は有効な浮動小数点数(float)ではありません。");
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
                    if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
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

        #endregion
    }
}
