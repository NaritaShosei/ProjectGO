using System;
using System.Collections.Generic;
using UnityEngine;

// BossEnemy関連
using BossEnemy.Data;
using BossEnemy.Model.Interface;

namespace BossEnemy.Infrastructure.Repository
{
    public class BossEnemyAttackDataRepository : IBossEnemyAttackDataRepository
    {
        public void Init(string textAsset)
        {
            if (textAsset == null || string.IsNullOrEmpty(textAsset))
            {
                Debug.LogError("Boss attack CSV is not set.");
                _bossMasterData = new string[0, 0];
                _attackDataArrayStartNum = -1;
                return;
            }

            _bossMasterData = CSVDateLoader.ParseCsv(textAsset);
            _attackDataArrayStartNum = -1;


            for(int y = 0; y < _bossMasterData.GetLength(0); y++)
            {
                if (_bossMasterData[y,0] == _attackDataArrayStartKey)
                {
                    _attackDataArrayStartNum = y;
                    return;
                }
            }

            Debug.LogError("AttackData section was not found in boss CSV.");
        }

        public BossEnemyAttackData GetData(int id)
        {
            if (_attackDataDict.TryGetValue(id, out BossEnemyAttackData cachedData)) return cachedData;

            if (_attackDataArrayStartNum < 0)
            {
                Debug.LogError($"Boss attack data section is invalid. ID:{id}");
                return default;
            }

            if (_bossMasterData.GetLength(1) < _attackDataStringLength)
            {
                Debug.LogError($"Boss attack CSV columns are not enough. Required:{_attackDataStringLength}, Actual:{_bossMasterData.GetLength(1)}");
                return default;
            }

            int commentLineNum = 2;

            for (int x = _attackDataArrayStartNum + commentLineNum; x < _bossMasterData.GetLength(0); x++)
            {
                if (int.TryParse(_bossMasterData[x, 0], out int dataID) && dataID == id)
                {
                    string[] attackDataStrings = new string[_attackDataStringLength];
                    for (int i = 0; i < _attackDataStringLength; i++)
                    {
                        attackDataStrings[i] = _bossMasterData[x, i];
                    }

                    try
                    {
                        BossEnemyAttackData attackData = BuildAttackData(attackDataStrings);
                        _attackDataDict[id] = attackData;
                        return attackData;
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError($"Failed to build boss attack data. ID:{id}, Row:{x}");
                        Debug.LogException(exception);
                        return default;
                    }
                }
            }

            Debug.LogError($"Boss attack data was not found. ID:{id}");
            return default;
        }

        private Dictionary<int, BossEnemyAttackData> _attackDataDict = new();

        private string[,] _bossMasterData;

        private int _attackDataArrayStartNum;

        private const string _attackDataArrayStartKey = "AttackData";

        private const int _attackDataStringLength = 13;

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
