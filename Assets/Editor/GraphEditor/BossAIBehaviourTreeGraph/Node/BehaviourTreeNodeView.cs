using BossEnemy.AI.BehaviourTree;
using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace BossEnemy.AI.Editor.BehaviourGraph
{
    public interface IBehaviourTreeGraphNode
    {
        public bool IsVisible { get; }

        public TreeNode BehaviourTreeNode { get; }

        public List<string> ConnectedNodesKey { get; }

        public string NodeAccesskey { get; }

        void OnGraphChanged(GraphLogger graphLogger);

        public string CreateUniqueKey();

        /// <summary> 出力ポートに接続されている子ノードのTreeNodeを取得する </summary>
        List<TreeNode> GetConnectedChildren();
    }

    [Serializable]
    public abstract class BehaviourTreeNodeView<TTreeNode> : Node, IBehaviourTreeGraphNode where TTreeNode : TreeNode
    {
        public const string ENTRY_PORT_NAME = "親ノード接続";

        public const string CHILD_PORT_NAME = "子ノード接続";

        public const string RUNNING_PRIORITY_OPTION_NAME = "実行優先度：";

        public List<string> ConnectedNodesKey => _connectedNodesKey;

        public TreeNode BehaviourTreeNode => _behaviourTreeNode;

        public bool IsVisible => _isVisible;

        public string NodeAccesskey => _nodeAccesskey;

        public virtual void OnGraphChanged(GraphLogger graphLogger)
        {
            ResetConnectedNodes();

            if (GetNodeOptionByName(RUNNING_PRIORITY_OPTION_NAME).TryGetValue(out int runningPriority))
            {
                _behaviourTreeNode.SetRunningPriority(runningPriority);
            }
        }

        /// <summary> 出力ポートに接続されている子ノードの BehaviourTreeNode を取得する </summary>
        public virtual List<TreeNode> GetConnectedChildren()
        {
            var connectTreeNodes = new List<TreeNode>();
            if (outputPortCount == 0) return connectTreeNodes;

            IPort childNodePort = GetOutputPortByName(CHILD_PORT_NAME);
            if (childNodePort == null) return connectTreeNodes;

            var connectedPorts = new List<IPort>();
            childNodePort.GetConnectedPorts(connectedPorts);

            foreach (var port in connectedPorts)
            {
                INode node = port.GetNode();
                if (node is IBehaviourTreeGraphNode graphNode &&
                    graphNode.IsVisible &&
                    graphNode.BehaviourTreeNode != null)
                {
                    if (!connectTreeNodes.Contains(graphNode.BehaviourTreeNode))
                    {
                        connectTreeNodes.Add(graphNode.BehaviourTreeNode);
                    }
                }
            }

            connectTreeNodes.Sort((a, b) => a.RunningPriority.CompareTo(b.RunningPriority));
            return connectTreeNodes;
        }

        /// <summary> Node間のつながりをリセットする </summary>
        protected void ResetConnectedNodes()
        {
            _connectedNodesKey.Clear();
        }

        /// <summary> 確実にほかのノードと被らないユニークなKeyを作る </summary>
        public string CreateUniqueKey()
        {
            return Guid.NewGuid().ToString(); 
        }

        protected List<string> _connectedNodesKey = new List<string>();

        protected TTreeNode _behaviourTreeNode;

        [SerializeField] protected string _nodeAccesskey;

        private bool _isVisible = true;

        public override void OnEnable()
        {
            _isVisible = true;

            if (string.IsNullOrEmpty(_nodeAccesskey))
            {
                _nodeAccesskey = CreateUniqueKey();
            }
        }

        public override void OnDisable()
        {
            _isVisible = false;
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<int>(RUNNING_PRIORITY_OPTION_NAME).Build();
        }
    }

    public class GraphChangedSaveNode : Node
    {
        public const string IS_SAVE_GARPH_CHANGED = "グラフの変更情報をリポジトリへオートセーブを有効にするフラグ";

        public bool IsGraphChangedSave => _isSave;

        public void OnGraphChanged(GraphLogger graphLogger)
        {
            if (GetNodeOptionByName(IS_SAVE_GARPH_CHANGED).TryGetValue(out bool isSave))
            {
                _isSave = isSave;
            }
        }

        private bool _isSave;

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<bool>(IS_SAVE_GARPH_CHANGED).Build();
        }
    }
}
