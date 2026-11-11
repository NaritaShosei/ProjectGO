using Boss.AI.BehaviourTree;
using Boss.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.VisualScripting;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Boss.Infrastructure.Repository
{
    /// <summary> 構築したビヘイビアツリーのNodeを管理するリポジトリクラス </summary>
    [CreateAssetMenu(fileName = "BossAIBehaviourTreeNodeRepositry", menuName = "Repository/BossAIBehaviourTreeNodeRepositry")]
    public class BossAIBehaviourTreeNodeRepositry : ScriptableObject, IBossAIBehaviourTreeNodeRepository
    {
        #region ノード保管用の構造体
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
        #endregion

        /// <summary> BehaviourTree最上部のEntryNodeを取得する </summary>
        public bool TryGetEntryNode(int id, out EntryNode entryNode)
        {
            // もしEntryNodeのオブジェクトプール内に欲しいNodeが存在していればプール内からノードを取得してentryNodeに返す
            if (_cloneNodesPool.ContainsKey(id))
            {
                if(_cloneNodesPool[id].TryDequeue(out EntryNode result))
                {
                    entryNode = result;
                    return true;
                }
            }

            // ノードの保管クラスからエントリーノードを取得して、クローンを生成してentryNodeに返す
            foreach (NodesHolder nodeHolder in _nodesHolder)
            {
                if (nodeHolder.EntryNode == null) continue;

                if (nodeHolder.EntryNode.BehaviourTreeID == id)
                {
                    entryNode = GetBehaviourTreeNodeClone(nodeHolder.EntryNode);
                    return true;
                }
            }

            // 探しているIDのノードが存在しない場合に到達し、nullとfalseを返す
            entryNode = null;
            return false;
        }

        /// <summary> 使用済みノードの返却 </summary>
        public void ReleaseNode(EntryNode releaseNode)
        {
            if (!_cloneNodesPool.ContainsKey(releaseNode.BehaviourTreeID))
                _cloneNodesPool.Add(releaseNode.BehaviourTreeID, new Queue<EntryNode>());

            _cloneNodesPool[releaseNode.BehaviourTreeID].Enqueue(releaseNode);
        }

        #region EditorGraph上でBehaviourTreeの変更を保存するためのメソッド
#if UNITY_EDITOR
        /// <summary> 新しいBehaviourTreeのEntryNodeを保存する </summary>
        public void SaveEntryNode(EntryNode newEntryNode)
        {
            if (newEntryNode == null) return;

            foreach (var nodes in _nodesHolder)
            {
                if (nodes.EntryNode != null &&
                    nodes.EntryNode.BehaviourTreeID == newEntryNode.BehaviourTreeID)
                {
                    nodes.SetEntryNode(newEntryNode);
                    EditorUtility.SetDirty(this);
                    return;
                }
            }

            var holder = new NodesHolder();
            holder.SetEntryNode(newEntryNode);
            holder.SetChildrenNodes(new TreeNode[0]);
            _nodesHolder.Add(holder);
            EditorUtility.SetDirty(this);
        }

        /// <summary> 新たなノードをBehaviourTreeに追加する </summary>
        public void AddTreeNode(EntryNode entryNode, TreeNode node)
        {
            // EntryNode is the tree root and is stored separately in NodesHolder.
            // Adding it here creates a self-reference when this data is serialized.
            if (entryNode == null || node == null || ReferenceEquals(entryNode, node)) return;

            foreach (var nodes in _nodesHolder)
            {
                if (nodes.EntryNode != null &&
                    nodes.EntryNode.BehaviourTreeID == entryNode.BehaviourTreeID)
                {
                    TreeNode[] childrenNodes = nodes.ChildrenNodes ?? Array.Empty<TreeNode>();
                    if (childrenNodes.Contains(node)) return;

                    List<TreeNode> treeNodes = childrenNodes.ToList();
                    treeNodes.Add(node);
                    nodes.SetChildrenNodes(treeNodes.ToArray());
                    EditorUtility.SetDirty(this);
                    return;
                }
            }

            var holder = new NodesHolder();
            holder.SetEntryNode(entryNode);
            holder.SetChildrenNodes(new[] { node });
            _nodesHolder.Add(holder);
            EditorUtility.SetDirty(this);
        }

        /// <summary> 使わなくなったノードをTreeから削除する </summary>
        public void RemoveTreeNode(EntryNode entryNode, TreeNode node)
        {
            foreach (var nodes in _nodesHolder)
            {
                if (nodes.EntryNode != null &&
                    nodes.EntryNode.BehaviourTreeID == entryNode.BehaviourTreeID)
                {
                    TreeNode[] childrenNodes = nodes.ChildrenNodes ?? Array.Empty<TreeNode>();
                    if (!childrenNodes.Contains(node)) return;

                    List<TreeNode> treeNodes = childrenNodes.ToList();
                    treeNodes.Remove(node);
                    nodes.SetChildrenNodes(treeNodes.ToArray());
                    EditorUtility.SetDirty(this);
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
                    nodes.EntryNode.BehaviourTreeID == entryNode.BehaviourTreeID)
                {
                    nodes.SetChildrenNodes(newChildren);
                    EditorUtility.SetDirty(this);
                    return;
                }
            }

            var holder = new NodesHolder();
            holder.SetEntryNode(entryNode);
            holder.SetChildrenNodes(newChildren);
            _nodesHolder.Add(holder);
            EditorUtility.SetDirty(this);
        }
#else
        public void SaveEntryNode(EntryNode newEntryNode) { }
        public void AddTreeNode(EntryNode entryNode, TreeNode node) { }
        public void RemoveTreeNode(EntryNode entryNode, TreeNode node) { }
        public void SyncTreeNodes(EntryNode entryNode, IEnumerable<TreeNode> connectedNodes) { }
#endif
        #endregion

        [Header("EditorGraph上で組んだBehaviourTreeを保管する変数")]
        [SerializeField] private List<NodesHolder> _nodesHolder = new();

        // 使用済みのノードのクローンを保管するオブジェクトプール
        private readonly Dictionary<int, Queue<EntryNode>> _cloneNodesPool = new();

        /// <summary> プロトタイプをもとに新たなビヘイビアツリーノードを生成、取得を行う </summary>
        /// <param name="prototype"> クローン元のプロトタイプノード </param>
        /// <returns> クローンしたビヘイビアツリーノード </returns>
        private EntryNode GetBehaviourTreeNodeClone(EntryNode prototype)
        {
            // 子ノード未設定の複製ノードをまとめたDictionary
            Dictionary<TreeNode, TreeNode[]> notSetChildrenCloneNodeDict = new Dictionary<TreeNode, TreeNode[]>();

            // EntryNodeのクローンを生成
            EntryNode cloneEntryNode = (EntryNode)prototype.Clone();
            notSetChildrenCloneNodeDict.Add(cloneEntryNode, prototype.Children);

            // クローン対象のノードが0になるまでループ
            while (notSetChildrenCloneNodeDict.Count != 0)
            {
                // クローンを生成済みのノードをまとめたリスト
                List<TreeNode> clonedNodes = new List<TreeNode>();

                // 子ノード設定済みノードリスト
                List<TreeNode> setChildrenNodes = new List<TreeNode>();

                // 次のループで子ノード設定を行うノードのリスト
                Dictionary<TreeNode, TreeNode[]> setChildrenNodesInNextLoopDist = new Dictionary<TreeNode, TreeNode[]>();

                // クローン対象からクローンを生成
                foreach (var setChildrenTarget in notSetChildrenCloneNodeDict.Keys)
                {
                    // 子ノード設定済みListにターゲットクローンを追加
                    setChildrenNodes.Add(setChildrenTarget);

                    // クローン対象に子ノードが含まれていなければループを再開
                    if (notSetChildrenCloneNodeDict[setChildrenTarget] == null ||
                        notSetChildrenCloneNodeDict[setChildrenTarget].Length == 0)
                    {
                        Debug.Log($"{ setChildrenTarget.GetType() } 子ノードの存在が確認されませんでした、よって終着点となります");
                        continue;
                    }
                    
                    // クローンしたノードの子ノードクローンを補完する配列
                    TreeNode[] children = new TreeNode[notSetChildrenCloneNodeDict[setChildrenTarget].Length];

                    // 次のクローン対象となる子ノードをListに追加
                    for(int index = 0; index < children.Length; index++)
                    {
                        // クローン済みのノードを複製し、クローン複製の無限ループが起こるケースの削除
                        if (clonedNodes.Contains(notSetChildrenCloneNodeDict[setChildrenTarget][index]))
                        {
                            throw new Exception("クローン済みの上位ノードが下位ノードとしてクローン対象に指定されています。");
                        }

                        // 子ノードを生成
                        TreeNode childClone = notSetChildrenCloneNodeDict[setChildrenTarget][index].Clone();

                        // 子ノードを子ノードクローンを補完する配列に追加
                        children[index] = childClone;

                        // 生成した子ノードを次の子ノードクローン設定対象にする
                        setChildrenNodesInNextLoopDist.Add(childClone, notSetChildrenCloneNodeDict[setChildrenTarget][index].Children);

                        // クローン済みのノードをクローン済みノードリストに追加する
                        clonedNodes.Add(notSetChildrenCloneNodeDict[setChildrenTarget][index]);
                    }

                    // 子ノードを設定
                    setChildrenTarget.SetChildren(children);
                }

                // 子ノード設定済みノードがなければそのままループを再開
                if (setChildrenNodes.Count == 0) continue;

                // 子ノード設定済みノードを子ノード設定ターゲットから削除
                foreach (var setChildrenNode in setChildrenNodes)
                {
                    notSetChildrenCloneNodeDict.Remove(setChildrenNode);
                }

                // 次のループで子ノード設定を行うノードがなければそのままループを再開
                if (setChildrenNodesInNextLoopDist.Count == 0) continue;

                // 次のループで子ノード設定を行うノードを子ノード設定ターゲット
                foreach (var setChildrenNode in setChildrenNodesInNextLoopDist.Keys)
                {
                    notSetChildrenCloneNodeDict.Add(setChildrenNode, setChildrenNodesInNextLoopDist[setChildrenNode]);
                }
            }

            // Cloneしたノードを返す
            return cloneEntryNode;
        }
    }
}
