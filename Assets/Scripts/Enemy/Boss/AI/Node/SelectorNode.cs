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

            // 入力用のポートを作成
            var inputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(float)); // 第三引数をPort.Capacity.Multipleにすると複数のポートへの接続が可能になる
            inputPort.portName = "Input";
            inputContainer.Add(inputPort); // 入力用ポートはinputContainerに追加する

            // 出力用のポートを作る
            var outputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
            outputPort.portName = "Value";
            outputContainer.Add(outputPort); // 出力用ポートはoutputContainerに追加する
        }

        public override NodeCondition TryEntry()
        {
            return NodeCondition.Success;
        }

        public override NodeCondition TryEntryNextNode(ITreeNode nextNode)
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

            Debug.LogError("すべてのノードに入れませんでした");

            foreach (var child in _childrenNode)
            {
                Debug.Log(child.GetType().Name);
            }
            return NodeCondition.Failure;
        }

        /// <summary> 子ノード </summary>
        private ITreeNode[] _childrenNode = null;
    }
    #endregion
}
