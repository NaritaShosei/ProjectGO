using BossEnemy.Armor;
using BossEnemy.Character;
using BossEnemy.Enum;
using BossEnemy.Interface;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace BossEnemy.Infrastructure.Repository
{
    [CreateAssetMenu(fileName = "BossCharacterEntityRepository", menuName = "Repository/BossCharacterEntityRepository")]
    public class BossCharacterEntityRepository : ScriptableObject, IBossCharacterEntityRepository
    {
        public string CSVDataSearchStartKey => "BossStatus";

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

            _bossCharacterEntityDict.Clear();

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

            Debug.Log($"Boss CSV 範囲: Row {_csvDataSearchStartRow} ～ {_csvDataSearchEndRow}");
        }

        public BossCharacterEntity GetEntity(int id)
        {
            if (_csvMasterData == null)
            {
                throw new InvalidOperationException("BossEnemyEntityRepository.Init() が呼ばれていません。");
            }

            if (_bossCharacterEntityDict.TryGetValue(id, out var cachedEntity)) return cachedEntity;

            for (int row = _csvDataSearchStartRow + 1; row < _csvDataSearchEndRow; row++)
            {
                // ID は先頭列、名前は2列目
                if (!int.TryParse(GetCell(row, 0), out int foundId) || foundId != id) continue;

                string characterName = GetCell(row, 1);
                if (string.IsNullOrWhiteSpace(characterName))
                {
                    throw new InvalidOperationException($"Boss ID {id} の名前が空です。Row: {row}");
                }

                var entity = CreateEntity(characterName, row + 1);

                entity.Init();

                _bossCharacterEntityDict.Add(id, entity);
                return entity;
            }

            Debug.LogError($"Bossデータの取得に失敗しました。ID: {id}");
            return null;
        }

        [SerializeField] private TextAsset _masterDataSheet;
        private string[,] _csvMasterData;
        private int _csvDataSearchStartRow;
        private int _csvDataSearchEndRow;
        private readonly Dictionary<int, BossCharacterEntity> _bossCharacterEntityDict = new();

        private BossCharacterEntity CreateEntity(string characterName, int firstDataRow)
        {
            var statusList = new List<CharacterStatus>();

            int row = firstDataRow;

            while (row < _csvDataSearchEndRow)
            {
                // 次の Boss ID に達したら終了
                if (int.TryParse(GetCell(row, 0), out _)) break;

                // Phase 行では2列目に Phase 番号がある
                if (!int.TryParse(GetCell(row, 1), out int phase))
                {
                    row++;
                    continue;
                }

                int maxHp = ParseInt(row, 2, "最大HP");
                int hardDef = ParseInt(row, 3, "急所(超柔らかい)の肉質");
                int normalDef = ParseInt(row, 4, "弱点の肉質");
                int weakPointDef = ParseInt(row, 5, "通常の肉質");
                int vitalPointDef = ParseInt(row, 6, "硬い肉質");
                float walkSpeed = ParseFloat(row, 7, "移動速度");

                var bodyPartsDefense = new Dictionary<TakeDamageType, int>
                {
                    { TakeDamageType.Hard, hardDef },
                    { TakeDamageType.Normal, normalDef },
                    { TakeDamageType.WeekPoint, weakPointDef },
                    { TakeDamageType.VitalPoint, vitalPointDef }
                };

                row++;

                // Phase 行の直後に続く Armor 行を読む
                var armorStats = new Dictionary<ArmorAttachmentType, ArmorStatus>();

                while (row < _csvDataSearchEndRow && System.Enum.TryParse(GetCell(row, 1), out ArmorAttachmentType attachmentType))
                {
                    int armorHp = ParseInt(row, 2, "鎧HP");
                    int armorDef = ParseInt(row, 3, "鎧の肉質");

                    armorStats.Add( attachmentType, new ArmorStatus(armorHp, armorDef));
                    row++;
                }

                statusList.Add(new CharacterStatus(phase, maxHp, walkSpeed, bodyPartsDefense, armorStats));
            }

            if (statusList.Count == 0)
                throw new InvalidOperationException($"{characterName} の Phase データを1件も取得できませんでした。");

            return new BossCharacterEntity(characterName, statusList.ToArray());
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
