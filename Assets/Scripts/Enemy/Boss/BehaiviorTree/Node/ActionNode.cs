using BossEnemy.Data;
using BossEnemy.Data.Repositry;
using UnityEngine;

namespace BossEnemy.BehaviorTree.Node.Action
{
    /// <summary> 行動の実行を行うNode(最終的なTree構造の最深部) </summary>
    public abstract class ActionNode : TreeNodeBase
    {
        public NodeCondition CurrentCondition => _currentCondition;

        protected NodeCondition _currentCondition = NodeCondition.Ready;
    }

    /// <summary> 攻撃Node </summary>
    public class AttackAction : ActionNode
    {
        public AttackAction(BossEnemyAttackField bossEnemyAttackField, BossEnemyAttackDataRepositry bossEnemyAttackDataRepositry)
        {
            _bossEnemyAttackField = bossEnemyAttackField;
        }

        public override NodeCondition TryEntry()
        {
            return NodeCondition.Failure;
        }

        private BossEnemyAttackField _bossEnemyAttackField;
    }

    /// <summary> ボスのPhase変更時のNode </summary>
    public class PhaseChange : ActionNode
    {
        public override NodeCondition TryEntry()
        {
            Debug.Log("つぎのPhaseへ");
            return NodeCondition.Success;
        }
    }

    /// <summary> ボス討伐成功時のNode </summary>
    public class DefeatBoss : ActionNode
    {
        public override NodeCondition TryEntry()
        {
            Debug.Log("Boss撃破");
            return NodeCondition.Success;
        }
    }
}
