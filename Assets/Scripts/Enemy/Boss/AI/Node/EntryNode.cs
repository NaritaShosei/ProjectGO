using UnityEngine;

namespace BossEnemy.AI
{
    public class EntryNode : TreeNodeBase
    {
        public override NodeCondition TryEntry()
        {
            return NodeCondition.Success;
        }

        public override NodeCondition TryEntryNextNode(out ITreeNode nextNode)
        {
            if (_childNode == null)
            {
                Debug.LogError("子ノードが設定されていません");
                nextNode = null;
                return NodeCondition.Failure;
            }

            nextNode = _childNode;
            return NodeCondition.Success;
        }

        /// <summary> 子ノード </summary>
        private ITreeNode _childNode = null;
    }
}
