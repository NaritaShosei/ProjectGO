using System;
using UnityEditor;
using UnityEngine;

namespace BossEnemy.AI.BehaviourTree
{
    #region 子ノードを順番に実行して一番最初にSuccessになったNodeを実行するSelectorNode
    /// <summary> 子ノードを順番に実行して一番最初にSuccessになったNodeを実行する </summary>
    [Serializable]
    public class SelectorNode : BossCharacterBehaviourTreeNode
    {
        public override NodeCondition TryEntry()
        {
            return NodeCondition.Success;
        }

        public override NodeCondition TryEntryNextNode(out ITreeNode nextNode)
        {
            foreach (var child in _childrenNode)
            {
                NodeCondition childCondition = child.TryEntry();

                if (childCondition == NodeCondition.Success)
                {
                    nextNode = child;
                    Debug.Log("ノードの選択に成功しました");
                    return NodeCondition.Success;
                }

                if (childCondition == NodeCondition.Running)
                {
                    nextNode = child;
                    Debug.Log("ノードの選択に成功しました");
                    return NodeCondition.Running;
                }
            }

            Debug.LogError("ノードの選択に失敗しました");
            nextNode = null;
            return NodeCondition.Failure;
        }
    }
    #endregion
}
