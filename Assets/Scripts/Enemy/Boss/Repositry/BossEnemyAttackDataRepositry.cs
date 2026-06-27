using System.Collections.Generic;
using UnityEngine;

namespace BossEnemy.Data.Repositry
{
    public class BossEnemyAttackDataRepositry
    {
        public BossEnemyAttackDataRepositry(TextAsset textAsset)
        {
            _bossMasterData = CSVDateLoader.ParseCsv(textAsset.text);


            for(int y = 0; y < _bossMasterData.GetLength(0); y++)
            {
                if (_bossMasterData[y,0] == _attackDataArrayStartKey)
                {
                    _attackDataArrayStartNum = y;
                    return;
                }
            }
        }

        public BossEnemyAttackData GetData(int id)
        {
            if (_attackDataDict.ContainsKey(id)) return _attackDataDict[id];

            int commentLineNum = 2;

            for (int x = _attackDataArrayStartNum + commentLineNum; x < _bossMasterData.GetLength(0); x++)
            {
                if (int.TryParse(_bossMasterData[x, 0], out int dataID) && dataID == id)
                {
                    string[] attackDataStrings = new string[_attackDataStringLength]
                    {
                        _bossMasterData[x, 0],
                        _bossMasterData[x, 1],
                        _bossMasterData[x, 2],
                        _bossMasterData[x, 3],
                        _bossMasterData[x, 4],
                        _bossMasterData[x, 5],
                        _bossMasterData[x, 6],
                        _bossMasterData[x, 7],
                        _bossMasterData[x, 8],
                        _bossMasterData[x, 9],
                        _bossMasterData[x, 10],
                        _bossMasterData[x, 11]
                    };

                    return BuildAttackData(attackDataStrings);
                }
            }

            return default;
        }

        private Dictionary<int, BossEnemyAttackData> _attackDataDict = new();

        private readonly string[,] _bossMasterData;

        private readonly int _attackDataArrayStartNum;

        private const string _attackDataArrayStartKey = "AttackData";

        private const int _attackDataStringLength = 12;

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
                attackDataStrings[11]
            );

        }
    }
}
