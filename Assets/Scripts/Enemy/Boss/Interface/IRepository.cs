using BossEnemy.Character;

namespace BossEnemy.Interface
{
    public interface IRepository<T>
    {
        public T GetData(int id);
    }

    public interface IBossEnemyAttackDataRepository : IRepository<Attack.AttackData>
    {
        public void Init();
    }

    public interface IBossEnemyDataRepository : IRepository<BossCharacterEntity>
    {
        public void Init();
    }
}
