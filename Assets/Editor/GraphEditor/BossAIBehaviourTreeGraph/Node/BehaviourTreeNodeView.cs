using Boss.AI.BehaviourTree;
using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace Boss.AI.Editor.BehaviourGraph
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

        /// <summary> Graphに変更があった際の処理 </summary>
        public virtual void OnGraphChanged(GraphLogger graphLogger)
        {
            ResetConnectedNodes();

            if (GetNodeOptionByName(RUNNING_PRIORITY_OPTION_NAME).TryGetValue(out int runningPriority))
            {
                Debug.Assert(runningPriority >= 0, $"実行優先度にマイナスの値が設定されています");

                _behaviourTreeNode.SetRunningPriority((uint)runningPriority);
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
                    if (graphNode.BehaviourTreeNode is TreeNode treeNode)
                    {
                        if (!connectTreeNodes.Contains(treeNode)) connectTreeNodes.Add(treeNode);
                    }
                    else Debug.LogError("graphNode.BehaviourTreeNodeのTreeNodeへの型変換が失敗しました");
                }
            }

            connectTreeNodes.Sort((a, b) => a.RunningPriority.CompareTo(b.RunningPriority));
            return connectTreeNodes;
        }

        /// <summary> 確実にほかのノードと被らないユニークなKeyを作る </summary>
        public string CreateUniqueKey()
        {
            return Guid.NewGuid().ToString(); 
        }

        [Header("各ノードが持つユニークな接続コード")]
        [SerializeField] protected string _nodeAccesskey;

        // 接続先の接続コードリスト
        protected List<string> _connectedNodesKey = new List<string>();

        // Runtimeで使用するTreeNode
        protected TTreeNode _behaviourTreeNode;

        // 現在の表示状況
        private bool _isVisible = true;

        /// <summary> NodeにOptionを追加する </summary>
        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<int>(RUNNING_PRIORITY_OPTION_NAME).Build();
        }

        /// <summary> Node間のつながりをリセットする </summary>
        protected void ResetConnectedNodes()
        {
            _connectedNodesKey.Clear();
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
