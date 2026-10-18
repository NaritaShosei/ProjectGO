using System;
using UnityEngine;

namespace Boss.AI.BehaviourTree
{
    #region 子ノードを順番に実行して一番最初にSuccessになったNodeを実行するSelectorNode
    /// <summary> 子ノードを順番に実行して一番最初にSuccessになったNodeを実行する </summary>
    [Serializable]
    public class SelectorNode : BossAITreeNode
    {
        public override NodeCondition TryEntry()
        {
            return NodeCondition.Success;
        }

        public override NodeCondition TryEntryNextNode(out TreeNode nextNode)
        {
            foreach (var child in _childrenNode)
            {
                NodeCondition childCondition = child.TryEntry();

                if (childCondition == NodeCondition.Success)
                {
                    nextNode = child;
                    return NodeCondition.Success;
                }

                if (childCondition == NodeCondition.Running)
                {
                    nextNode = child;
                    return NodeCondition.Running;
                }
            }

            Debug.LogError("ノードの選択に失敗しました");
            nextNode = null;
            return NodeCondition.Failure;
        }

        public override TreeNode Clone()
        {
            return new SelectorNode();
        }
    }
    #endregion
}
