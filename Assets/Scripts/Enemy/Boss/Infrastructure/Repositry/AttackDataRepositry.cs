using BossEnemy.Character;
using BossEnemy.Interface;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor.Build.Pipeline.Tasks;
using UnityEngine;

namespace BossEnemy.Infrastructure.Repository
{
    [CreateAssetMenu(fileName = "BossAttackDataRepositry", menuName = "Repository/BossAttackDataRepositry")]
    public class AttackDataRepositry : ScriptableObject ,IBossEnemyAttackDataRepository
    {
        public string CSVDataSearchStartKey => "AttackData";

        /// <summary> 初期化 </summary>
        public void Init()
        {
            if (_masterDataSheet == null)
            {
                throw new InvalidOperationException("MasterDataSheetが設定されていません。");
            }

            _csvMasterData = CSVDateLoader.ParseCsv(_masterDataSheet.text);
            if (_csvMasterData == null)
            {
                throw new InvalidOperationException("CSV の読み込みに失敗しました。");
            }

            _attackDataDict.Clear();
            bool foundStart = false;

            for (int row = 0; row < _csvMasterData.GetLength(0); row++)
            {
                string firstCell = GetCell(row, 0);
                if (!foundStart && firstCell == CSVDataSearchStartKey)
                {
                    _csvDataSearchStartColumn = row;
                    foundStart = true;
                    continue;
                }

                if (foundStart && firstCell == ICSVDataLoadRepository.CSV_DATA_SEARCH_END_KEY)
                {
                    _csvDataSearchEndColumn = row;
                    break;
                }
            }

            if (!foundStart || _csvDataSearchEndColumn <= _csvDataSearchStartColumn)
            {
                throw new InvalidOperationException($"CSV に {CSVDataSearchStartKey} の有効なデータ範囲がありません。");
            }
        }

        public Attack.AttackData GetData(int id)
        {
            int idColumn = 0;

            if (_attackDataDict.ContainsKey(id))
            {
                return _attackDataDict[id];
            }

            for (int row = _csvDataSearchStartColumn + 1; row < _csvDataSearchEndColumn; row++)
            {
                if (int.TryParse(_csvMasterData[row, idColumn], out int result) && id == result)
                {
                    var data = CreateData(row);
                    _attackDataDict.Add(id, data);
                    return data;
                }
            }

            Debug.LogError("データ取得に失敗しました");
            return default;
        }

        [SerializeField] private TextAsset _masterDataSheet;

        private string[,] _csvMasterData;

        private int _csvDataSearchStartColumn;

        private int _csvDataSearchEndColumn;

        private Dictionary<int, Attack.AttackData> _attackDataDict = new();

        /// <summary> AttackDataをCSVデータから作るメソッド </summary>
        /// <param name="row"> データの記述された行 </param>
        private Attack.AttackData CreateData(int row)
        {

            int id = ParseInt(row, 0, "ID");
            string name = GetCell(row, 1);
            float damage = ParseFloat(row , 2, "Damage");
            float hitAreaRadius = ParseFloat(row, 3, "HitAreaRadius");
            float attackStartDistance = ParseFloat(row, 4, "AttackStartDistance");
            DamageReactionType damageReactionType = System.Enum.Parse<DamageReactionType>(GetCell(row, 5));
            float coolTime = ParseFloat(row, 6, "CoolTime");
            string animParam = GetCell(row, 7);

            Attack.AttackData attackData = 
                new Attack.AttackData(id, name, damage, 
                hitAreaRadius, attackStartDistance, 
                damageReactionType, coolTime, animParam);

            return attackData;
        }

        private int ParseInt(int row, int column, string label)
        {
            string value = GetCell(row, column);

            if (!int.TryParse(value, out int result))
            {
                throw new InvalidOperationException($"{label} の読み込みに失敗しました。Row: {row}, Column: {column}, Value: '{value}'");
            }

            return result;
        }

        private float ParseFloat(int row, int column, string label)
        {
            string value = GetCell(row, column);

            if (!float.TryParse(
                    value,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float result))
            {
                throw new InvalidOperationException($"{label} の読み込みに失敗しました。Row: {row}, Column: {column}, Value: '{value}'");
            }

            return result;
        }

        private string GetCell(int row, int column)
        {
            if (row < 0 || row >= _csvMasterData.GetLength(0) ||
                column < 0 || column >= _csvMasterData.GetLength(1))
            {
                throw new IndexOutOfRangeException($"CSVの範囲外にアクセスしました Row: {row}, Column: {column}");
            }

            return _csvMasterData[row, column]?.Trim() ?? string.Empty;
        }
    }
}
