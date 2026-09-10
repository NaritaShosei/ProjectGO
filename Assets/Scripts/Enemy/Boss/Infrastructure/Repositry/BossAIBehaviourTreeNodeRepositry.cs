using BossEnemy.AI.BehaviourTree;
using BossEnemy.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BossEnemy.Infrastructure.Repository
{
    /// <summary> 構築したビヘイビアツリーのNodeを管理するリポジトリクラス </summary>
    [CreateAssetMenu(fileName = "BossAIBehaviourTreeNodeRepositry", menuName = "Repository/BossAIBehaviourTreeNodeRepositry")]
    public class BossAIBehaviourTreeNodeRepositry : ScriptableObject, IBossAIBehaviourTreeNodeRepository
    {
        [Serializable]
        public class NodesHolder
        {
            public EntryNode EntryNode => _entryNode;

            public TreeNode[] ChildrenNodes => _childrenNodes;

            public void SetEntryNode(EntryNode entryNode) => _entryNode = entryNode;

            public void SetChildrenNodes(TreeNode[] childrenNodes) => _childrenNodes = childrenNodes;

            [SerializeReference] private EntryNode _entryNode;

            [SerializeReference] private TreeNode[] _childrenNodes;
        }

        /// <summary> BehaviourTree最上部のEntryNodeを取得する </summary>
        public bool TryGetEntryNode(int id, out EntryNode entryNode)
        {
            foreach (NodesHolder node in _nodesHolder)
            {
                if (node.EntryNode == null) continue;

                if (node.EntryNode.PlayableCharactersID == id)
                {
                    entryNode = node.EntryNode;
                    return true;
                }
            }

            entryNode = null;
            return false;
        }

#if UNITY_EDITOR
        /// <summary> 新しいBehaviourTreeのEntryNodeを保存する </summary>
        public void SaveEntryNode(EntryNode newEntryNode)
        {
            if (newEntryNode == null) return;

            foreach (var nodes in _nodesHolder)
            {
                if (nodes.EntryNode != null &&
                    nodes.EntryNode.PlayableCharactersID == newEntryNode.PlayableCharactersID)
                {
                    nodes.SetEntryNode(newEntryNode);
                    MarkDirty();
                    return;
                }
            }

            var holder = new NodesHolder();
            holder.SetEntryNode(newEntryNode);
            holder.SetChildrenNodes(new TreeNode[0]);
            _nodesHolder.Add(holder);
            MarkDirty();
        }

        public void AddTreeNode(EntryNode entryNode, TreeNode node)
        {
            // EntryNode is the tree root and is stored separately in NodesHolder.
            // Adding it here creates a self-reference when this data is serialized.
            if (entryNode == null || node == null || ReferenceEquals(entryNode, node)) return;

            foreach (var nodes in _nodesHolder)
            {
                if (nodes.EntryNode != null &&
                    nodes.EntryNode.PlayableCharactersID == entryNode.PlayableCharactersID)
                {
                    TreeNode[] childrenNodes = nodes.ChildrenNodes ?? Array.Empty<TreeNode>();
                    if (childrenNodes.Contains(node)) return;

                    List<TreeNode> treeNodes = childrenNodes.ToList();
                    treeNodes.Add(node);
                    nodes.SetChildrenNodes(treeNodes.ToArray());
                    MarkDirty();
                    return;
                }
            }

            var holder = new NodesHolder();
            holder.SetEntryNode(entryNode);
            holder.SetChildrenNodes(new[] { node });
            _nodesHolder.Add(holder);
            MarkDirty();
        }

        public void RemoveTreeNode(EntryNode entryNode, TreeNode node)
        {
            foreach (var nodes in _nodesHolder)
            {
                if (nodes.EntryNode != null &&
                    nodes.EntryNode.PlayableCharactersID == entryNode.PlayableCharactersID)
                {
                    TreeNode[] childrenNodes = nodes.ChildrenNodes ?? Array.Empty<TreeNode>();
                    if (!childrenNodes.Contains(node)) return;

                    List<TreeNode> treeNodes = childrenNodes.ToList();
                    treeNodes.Remove(node);
                    nodes.SetChildrenNodes(treeNodes.ToArray());
                    MarkDirty();
                }
            }
        }

        /// <summary> EntryNodeに接続されているTreeNode群を同期する </summary>
        public void SyncTreeNodes(EntryNode entryNode, IEnumerable<TreeNode> connectedNodes)
        {
            if (entryNode == null) return;

            TreeNode[] newChildren = connectedNodes?
                .Where(n => n != null && !ReferenceEquals(n, entryNode))
                .Distinct()
                .ToArray() ?? Array.Empty<TreeNode>();

            foreach (var nodes in _nodesHolder)
            {
                if (nodes.EntryNode != null &&
                    nodes.EntryNode.PlayableCharactersID == entryNode.PlayableCharactersID)
                {
                    nodes.SetChildrenNodes(newChildren);
                    MarkDirty();
                    return;
                }
            }

            var holder = new NodesHolder();
            holder.SetEntryNode(entryNode);
            holder.SetChildrenNodes(newChildren);
            _nodesHolder.Add(holder);
            MarkDirty();
        }

        private void MarkDirty()
        {
            EditorUtility.SetDirty(this);
        }
#else
        public void SaveEntryNode(EntryNode newEntryNode) { }
        public void AddTreeNode(EntryNode entryNode, TreeNode node) { }
        public void RemoveTreeNode(EntryNode entryNode, TreeNode node) { }
        public void SyncTreeNodes(EntryNode entryNode, IEnumerable<TreeNode> connectedNodes) { }
#endif

        [SerializeField] private List<NodesHolder> _nodesHolder = new();
    }
}
