using BossEnemy.AI.BehaviourTree;
using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor;

namespace BossEnemy.AI.Editor.BehaviourGraph
{
    [Serializable]
    public class SequenceNode : BehaviourTreeNodeView<BehaviourTree.SequenceNode>
    {
        public SequenceNode()
        {
            _behaviourTreeNode = new BehaviourTree.SequenceNode();
            _nodeAccesskey = CreateUniqueKey();
        }

        public override void OnGraphChanged(GraphLogger graphLogger)
        {
            base.OnGraphChanged(graphLogger);

            // OutputPortが一つもなければ即座に次のループに移る
            if (outputPortCount == 0) return;

            // Nodeから子ノードへの接続を行うOutputPortを取得
            IPort childNodeEntryPort = GetOutputPortByName(CHILD_PORT_NAME);

            List<IPort> outConnectedPort = new List<IPort>();
            childNodeEntryPort.GetConnectedPorts(outConnectedPort);

            foreach (var edge in outConnectedPort)
            {
                if (_nodeAccesskey == edge.name) continue;
                _connectedNodesKey.Add(edge.name);
            }
        }

        private BehaviourTree.SequenceNode _sequenceNode = new();

        // ポートの定義
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            base.OnDefinePorts(context);

            context     
                .AddInputPort<TreeNode>(_nodeAccesskey)
                .WithDisplayName(ENTRY_PORT_NAME)
                .Build();

            context
                .AddOutputPort<TreeNode>(CHILD_PORT_NAME)
                .WithDisplayName(CHILD_PORT_NAME)
                .Build();
        }
    }
}
