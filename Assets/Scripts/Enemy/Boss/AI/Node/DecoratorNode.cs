using System;
using UniRx;
using UnityEngine;

using BossEnemy.Enum;
using BossEnemy.Attack;
using BossEnemy.Character;
using BossEnemy.Interface;

namespace BossEnemy.AI
{
    /// <summary> 通ったら子Nodeを実行して、通らなければFailureを返すNode </summary>
    public abstract class DecoratorNode : TreeNodeBase
    {
        public DecoratorNode(ITreeNode childNode)
        {
            _childNode = childNode;
        }

        public override NodeCondition TryEntryNextNode(out ITreeNode nextNode)
        {
            if(_childNode == null)
            {
                Debug.LogError("子ノードがNullです");
                nextNode = null;
                return NodeCondition.Failure;
            }

            NodeCondition condition = _childNode.TryEntry();

            if (condition == NodeCondition.Running || condition == NodeCondition.Success)
            {
                nextNode = _childNode;
                return NodeCondition.Success;
            }

            nextNode = null;
            return NodeCondition.Failure;
        }

        /// <summary> 子ノード </summary>
        protected ITreeNode _childNode = null;
    }
}
