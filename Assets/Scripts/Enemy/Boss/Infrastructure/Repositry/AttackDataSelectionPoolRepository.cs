using BossEnemy.Attack;
using BossEnemy.Interface;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace BossEnemy.Infrastructure.Repository
{
    [CreateAssetMenu(fileName = "AttackDataSelectionPoolRepository", menuName = "Repository/AttackDataSelectionPoolRepository")]
    public class AttackDataSelectionPoolRepository : ScriptableObject, IBossEnemyAttackSelectionPoolRepository
    {
        public string CSVDataSearchStartKey => "AttackSelectionPoolData";

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

            _attackDataSelectionPoolDict.Clear();

            bool foundStart = false;

            // CSV の先頭列を上から下へ走査する
            for (int row = 0; row < _csvMasterData.GetLength(0); row++)
            {
                string firstCell = GetCell(row, 0);

                if (!foundStart && firstCell == CSVDataSearchStartKey)
                {
                    _csvDataSearchStartRow = row;
                    foundStart = true;
                    continue;
                }

                if (foundStart && firstCell == ICSVDataLoadRepository.CSV_DATA_SEARCH_END_KEY)
                {
                    _csvDataSearchEndRow = row;
                    break;
                }
            }

            if (!foundStart)
            {
                throw new InvalidOperationException($"CSV に開始キー {CSVDataSearchStartKey} がありません。");
            }

            if (_csvDataSearchEndRow <= _csvDataSearchStartRow)
            {
                throw new InvalidOperationException($"開始キー {CSVDataSearchStartKey} より後にENDがありません。");
            }
        }

        public AttackSelectionPool GetSelectionPool(int id)
        {
            AttackSelectionPool attackSelectionPool = default;

            if (_csvMasterData == null)
            {
                throw new InvalidOperationException("Init() が呼ばれていません。");
            }

            if (_attackDataSelectionPoolDict.TryGetValue(id, out var cachedPool)) return cachedPool;

            for (int row = _csvDataSearchStartRow + 1; row < _csvDataSearchEndRow; row++)
            {
                // ID は先頭列、2列目以降は要素
                if (!int.TryParse(GetCell(row, 0), out int foundId) || foundId != id) continue;

                var pool = CreatePool(row);

                _attackDataSelectionPoolDict.Add(id, pool);
                return pool;
            }

            Debug.LogError($"AttackSelectPoolの取得に失敗しました。ID: {id}");
            return default;
        }

        [SerializeField] private TextAsset _masterDataSheet;
        private string[,] _csvMasterData;
        private int _csvDataSearchStartRow;
        private int _csvDataSearchEndRow;
        private readonly Dictionary<int, AttackSelectionPool> _attackDataSelectionPoolDict = new();

        private AttackSelectionPool CreatePool(int row)
        {
            bool isFinishAttackAddSelectionPool = false;
            AttackSelectionPool pool = new();
            List<AttackCondition> selectionField = new();

            int column = 1;
            while (isFinishAttackAddSelectionPool)
            {
                if (column < 0 || column >= _csvMasterData.GetLength(1) || 
                    !int.TryParse(GetCell(row, column), out int result))
                {
                    isFinishAttackAddSelectionPool = true;
                    continue;
                }

                int id = ParseInt(row, column, "攻撃ID");
                column++;

                int activationRate = ParseInt(row, column, "攻撃発生確率");
                column++;

                selectionField.Add(
                    new AttackCondition()
                {
                    ID = id,
                    ActivationRate = activationRate
                });
            }

            return pool;
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
