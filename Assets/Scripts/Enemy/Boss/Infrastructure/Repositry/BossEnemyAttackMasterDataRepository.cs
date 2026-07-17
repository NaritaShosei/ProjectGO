using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

// BossEnemy関連
using BossEnemy.Data;
using BossEnemy.Model.Interface;
using System.Threading.Tasks;
using Infrastructure;


namespace BossEnemy.Infrastructure.Repository
{
    [CreateAssetMenu(fileName = "BossEnemyAttackMasterDataRepository", menuName = "Repositry/BossEnemyMasterData")]
    public class BossEnemyAttackMasterDataRepository : ScriptableObject, IBossEnemyAttackDataRepository
    {
        public void Init()
        {
            _bossMasterData = CSVDateLoader.ParseCsv(_bossEnemyCsvTextAsset.text);

            for(int row = 0; row < _bossMasterData.GetLength(0); row++)
            {
                if(_bossMasterData[row, 0] == _attackDataRowStartKeyWord)
                {
                    _attackDataArrayStartNum = row;
                    return;
                }
            }
        }

        public BossEnemyAttackData GetData(int id)
        {
            if (_attackDataDict.TryGetValue(id, out BossEnemyAttackData cachedData)) return cachedData;

            if (_attackDataArrayStartNum < 0 || _bossMasterData.GetLength(1) < _csvAttackDataLength)
            {
                return default;
            }

            int commentLineNum = 2;

            for (int row = _attackDataArrayStartNum + commentLineNum; row < _bossMasterData.GetLength(0); row++)
            {
                if (int.TryParse(_bossMasterData[row, 0], out int dataID) && dataID == id)
                {
                    string[] attackDataStrings = new string[_csvAttackDataLength];
                    for (int i = 0; i < _csvAttackDataLength; i++)
                    {
                        attackDataStrings[i] = _bossMasterData[row, i];
                    }

                    try
                    {
                        BossEnemyAttackData attackData = BuildAttackData(attackDataStrings);
                        _attackDataDict[id] = attackData;
                        return attackData;
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError($"攻撃データの生成に失敗しました ID:{id}, 失敗した列:{row}");
                        Debug.LogException(exception);
                        return default;
                    }
                }
            }

            Debug.LogError($"指定されたIDのデータが見つかりませんでした ID:{id}");
            return default;
        }

        [SerializeField, Header("BossEnemyのCSV形式のマスターデータ")]
        private TextAsset _bossEnemyCsvTextAsset = null;

        private Dictionary<int, BossEnemyAttackData> _attackDataDict = new();

        private string[,] _bossMasterData;

        private int _attackDataArrayStartNum;

        private const string _attackDataRowStartKeyWord = "AttackData";

        private const int _csvAttackDataLength = 13;

        private BossEnemyAttackData BuildAttackData(string[] attackDataStrings)
        {
            return new BossEnemyAttackData
            (
                int.Parse(attackDataStrings[0]),
                attackDataStrings[1],
                float.Parse(attackDataStrings[2]),
                float.Parse(attackDataStrings[3]),
                float.Parse(attackDataStrings[4]),
                float.Parse(attackDataStrings[5]),
                float.Parse(attackDataStrings[6]),
                float.Parse(attackDataStrings[7]),
                float.Parse(attackDataStrings[8]),
                float.Parse(attackDataStrings[9]),
                float.Parse(attackDataStrings[10]),
                float.Parse(attackDataStrings[11]),
                attackDataStrings[12]
            );
        }
    }
}
