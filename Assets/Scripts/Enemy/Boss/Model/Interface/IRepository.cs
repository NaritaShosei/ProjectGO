using BossEnemy.Data;
using UnityEngine;

namespace BossEnemy.Model.Interface
{
    public interface IRepository<T>
    {
        public T GetData(int id);
    }

    public interface IBossEnemyAttackDataRepository : IRepository<BossEnemyAttackData>
    {
        public void Init(string textAsset);
    }

    public interface IBossEnemyMasterDataRepository : IRepository<BossEnemyMasterData>
    {
        public void Init(string csvText);
    }
}
