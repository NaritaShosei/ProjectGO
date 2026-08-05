using BossEnemy.Character;

namespace BossEnemy.Attack
{
    public class AttackInformationHolder
    {
        public BossEnemyAttackData AttackData => _attackData;

        public void SetData(BossEnemyAttackData attackData)
        {
            _attackData = attackData;
        }

        private BossEnemyAttackData _attackData;
    }
}
