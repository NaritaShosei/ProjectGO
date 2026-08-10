using BossEnemy.AI;
using BossEnemy.Character;

namespace BossEnemy.Interface
{
    public interface IBossEnemyAttackDataRepository
    {
        public void Init();

        public Attack.AttackData GetData(int id);
    }

    public interface IBossEnemyEntityRepository
    {
        public void Init();

        public BossCharacterEntity GetData(int id);
    }

    public interface IBossEnemyBehaviourTreeGraphRepository
    {
        public void Init();

        public ITreeNode GetEntryNode();
    }
}
