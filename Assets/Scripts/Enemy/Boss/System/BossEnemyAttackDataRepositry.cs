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
                if (_bossMasterData[y,0] == "AttackData")
                {
                    _attackDataArrayStartNum = y;
                    return;
                }
            }
        }

        public BossEnemyAttackData GetData(int id)
        {
            for (int y = _attackDataArrayStartNum; y < _bossMasterData.GetLength(0); y++)
            {

            }

            return default;
        }

        private readonly string[,] _bossMasterData;

        private readonly int _attackDataArrayStartNum;
    }
}
