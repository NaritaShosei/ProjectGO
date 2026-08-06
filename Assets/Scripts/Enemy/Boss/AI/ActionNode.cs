using UnityEngine;

using BossEnemy.Character;
using BossEnemy.System;

namespace BossEnemy.BehaviorTree
{
    /// <summary> 行動の実行を行うNode(最終的なTree構造の最深部) </summary>
    public abstract class ActionNode : TreeNodeBase
    {
        public override NodeCondition TryEntry()
        {
            return NodeCondition.Success;
        }

        public override NodeCondition TryGetNextNode(ref ITreeNode nextNode)
        {
            return NodeCondition.Running;
        }
    }
}
