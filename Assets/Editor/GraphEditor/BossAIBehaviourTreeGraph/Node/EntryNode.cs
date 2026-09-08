using BossEnemy.AI.BehaviourTree;
using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor;

namespace BossEnemy.AI.Editor.BehaviourGraph
{
    [Serializable]
    public class EntryNode : BehaviourTreeNodeView<BehaviourTree.EntryNode>
    {
        public EntryNode()
        {
            _behaviourTreeNode = new BehaviourTree.EntryNode();
        }

        private const string CHARACTER_ID_OPTION_NAME = "CharacterID";

        public override void OnGraphChanged(GraphLogger graphLogger)
        {
            ResetConnectedNodes();

            if (GetNodeOptionByName(CHARACTER_ID_OPTION_NAME).TryGetValue(out int characterID))
            {
                _behaviourTreeNode.SetPlayableCharacterID(characterID);
            }

            // OutputPortが一つもなければ即座に次のループに移る
            if (outputPortCount == 0) return;

            // Nodeから子ノードへの接続を行うOutputPortを取得
            IPort childNodeEntryPort = GetOutputPortByName(CHILD_PORT_NAME);

            List<IPort> outConnectedPort = new List<IPort>();
            childNodeEntryPort.GetConnectedPorts(outConnectedPort);

            foreach (var edge in outConnectedPort)
            {
                _connectedNodesKey.Add(edge.name);
            }
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            base.OnDefinePorts(context);

            context
                .AddOutputPort<TreeNode>(CHILD_PORT_NAME)
                .WithDisplayName(CHILD_PORT_NAME)
                .Build();
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<int>(CHARACTER_ID_OPTION_NAME).Build();
        }
    }
}
