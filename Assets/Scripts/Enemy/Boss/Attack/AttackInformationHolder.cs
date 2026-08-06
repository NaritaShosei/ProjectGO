using BossEnemy.Character;

namespace BossEnemy.Attack
{
    public class AttackInformationHolder
    {
        public AttackData AttackData => _attackData;

        public void SetData(AttackData attackData)
        {
            _attackData = attackData;
        }

        private AttackData _attackData;
    }
}
