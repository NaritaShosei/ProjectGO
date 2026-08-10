using BossEnemy.Character;
using System;

namespace BossEnemy.Attack
{
    public class BossAttackPresenter
    {
        public event Action<AttackData> OnAttackStart;

        public AttackData AttackData => _attackData;

        public void HnadleAttackStart(AttackData attackData)
        {
            _attackData = attackData;
        }

        private AttackData _attackData;
    }
}
