using BossEnemy.Character;

namespace BossEnemy.Interface
{
    public interface IRepository<T>
    {
        public T GetData(int id);
    }

    public interface IBossEnemyAttackDataRepository : IRepository<BossEnemyAttackData>
    {
        public void Init();
    }

    public interface IBossEnemyDataRepository : IRepository<BossEnemyMasterData>
    {
        public void Init();
    }
}
