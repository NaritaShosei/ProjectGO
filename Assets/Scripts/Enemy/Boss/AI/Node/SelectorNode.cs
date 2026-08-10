using BossEnemy.AI;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace BossEnemy.AI
{
    #region 子ノードを順番に実行して一番最初にSuccessになったNodeを実行するSelectorNode
    /// <summary> 子ノードを順番に実行して一番最初にSuccessになったNodeを実行する </summary>
    public class SelectorNode : TreeNodeBase
    {
        public SelectorNode()
        {
            title = "SelectorNode";

            // 親ノードからの入力用ポート
            var inputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(ITreeNode)); // 第三引数をPort.Capacity.Multipleにすると複数のポートへの接続が可能になる
            inputPort.portName = "EntryPort";
            inputContainer.Add(inputPort); // 入力用ポートはinputContainerに追加する

            // 子ノードへの出力用ポート
            var outputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(ITreeNode));
            outputPort.portName = "Child";
            outputContainer.Add(outputPort); // 出力用ポートはoutputContainerに追加する
        }

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

        /// <summary> 子ノード </summary>
        private ITreeNode[] _childrenNode = null;
    }
    #endregion
}
