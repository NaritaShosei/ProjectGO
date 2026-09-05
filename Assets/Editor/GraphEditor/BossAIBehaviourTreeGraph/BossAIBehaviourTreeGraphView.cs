using BossEnemy.AI.BehaviourTree;
using BossEnemy.Infrastructure.Repository;
using BossEnemy.Interface;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace BossEnemy.AI.Editor.BehaviourGraph
{
    #region BehaviorTreeを構築するTreeGraph生成用のEditor拡張
    /// <summary> BehaviorTreeを構築するGraphView </summary>
    [Serializable]
    [Graph(ASSET_EXTENSION)]
    public class BehaviourTreeGraphView : Graph
    {
        public const string ASSET_EXTENSION = "BossAIBehaviourTreeGraph";

        public event Action<BehaviourTree.EntryNode> OnChangedEntryNodes;

        [MenuItem("Assets/Create/GraphToolkit/BossAIBehaviourTreeGraph", false)]
        public static void CreateAssetFile()
        {
            GraphDatabase.PromptInProjectBrowserToCreateNewAsset<BehaviourTreeGraphView>();
        }

        public override void OnEnable()
        {
            base.OnEnable();

            Debug.Log("OnEnable");
            _presenter = new(this);

            // Graph上のすべてのノードを取得
            var nodes = GetNodes();

            foreach (var node in nodes)
            {
                TryAddBehaviourTreeNode(node);

                TryGetGraphChangedSaveNode(node);
            }
        }

        public override void OnDisable()
        {
            base.OnDisable();

            Debug.Log("OnDisable");
            _presenter.OnDisable();
            _presenter = null;
        }

        public override void OnGraphChanged(GraphLogger graphLogger)
        {
            if (_changedSaveNode == null) return;

            _changedSaveNode.OnGraphChanged(graphLogger);

            if (_changedSaveNode.IsGraphChangedSave)
            {
                Debug.Log("OnGraphChanged");

                // Graph上のすべてのノードを取得
                var nodes = GetNodes();

                foreach (var node in nodes)
                {
                    if (TryAddBehaviourTreeNode(node))
                    {
                        Debug.Log("新たなBehaviourTreeNodeを保存しました");
                    }
                }

                RefreshAllBehaviorTreeGraphNodes(graphLogger);

                //  EntryNodeを取得する
                if (!TryGetEntryNode()) return;

                // Graphから削除されたNodeを完全に取り除く
                RemoveUnwantedNodes();

                // 全てのbehaviourTreeNodeを更新する
                UpdateAllBehaviorTreeNodes(graphLogger);

                // EntryNodeから接続されているツリー内のノードを収集してリポジトリに同期
                SyncTreeNodesToRepository();

                _presenter.SaveRepository();

                Debug.Log("オートセーブが完了しました");
                return;
            }

            Debug.Log("オートセーブ機能がOFFになっています");
        }

        private GraphChangedSaveNode _changedSaveNode = null;

        private BehaviourTreeGraphPresenter _presenter = null;

        private BehaviourTree.EntryNode _entryNode;

        private List<IBehaviourTreeGraphNode> _behaviourTreeNodes = new List<IBehaviourTreeGraphNode>();

        /// <summary> EntryNodeを取得する </summary>
        private bool TryGetEntryNode()
        {
            bool isEntryNodeDuplicate = false;
            _entryNode = null;

            // EntryNodeのみ先に取得
            foreach (var behaviourTreeNode in _behaviourTreeNodes)
            {
                if (behaviourTreeNode.BehaviourTreeNode is BehaviourTree.EntryNode entry)
                {
                    if(_entryNode != null) isEntryNodeDuplicate = true;

                    _entryNode = entry;
                }
            }

            if (_entryNode == null)
            {
                Debug.LogError("EntryNodeが取得できませんでした、EntryNodeを生成してください。");
                return false;
            }

            if (isEntryNodeDuplicate)
            {
                Debug.LogError("EntryNodeが2つ以上存在しますEntryNodeは2つ以上存在できません");
                return false;
            }

            OnChangedEntryNodes?.Invoke(_entryNode);
            return true;
        }

        /// <summary> 削除されたNodeを完全に取り除く </summary>
        private void RemoveUnwantedNodes()
        {
            List<IBehaviourTreeGraphNode> removeNode = new List<IBehaviourTreeGraphNode>();
            foreach (var behaviourTreeNode in _behaviourTreeNodes)
            {
                if (!behaviourTreeNode.IsVisible)
                {
                    removeNode.Add(behaviourTreeNode);
                }
            }

            foreach (var removeTargetNode in removeNode)
            {
                if (removeTargetNode.BehaviourTreeNode != _entryNode)
                {
                    Debug.Log($" {removeTargetNode} を削除しました");
                }

                _behaviourTreeNodes.Remove(removeTargetNode);
            }
        }

        /// <summary> 全てのbehaviourTreeNodeを更新する </summary>
        private void RefreshAllBehaviorTreeGraphNodes(GraphLogger graphLogger)
        {
            foreach (var behaviourTreeNode in _behaviourTreeNodes)
            {
                behaviourTreeNode.OnGraphChanged(graphLogger);
            }
        }

        private void UpdateAllBehaviorTreeNodes(GraphLogger graphLogger)
        {
            foreach (var behaviourTreeNode in _behaviourTreeNodes)
            {
                if (!behaviourTreeNode.IsVisible || behaviourTreeNode.BehaviourTreeNode == null) continue;

                List<TreeNode> connectTreeNodes = behaviourTreeNode.GetConnectedChildren();
                behaviourTreeNode.BehaviourTreeNode.SetChildren(connectTreeNodes.ToArray());
            }
        }

        /// <summary> EntryNodeからポート接続を辿って到達可能なすべてのTreeNodeをリポジトリに同期する </summary>
        private void SyncTreeNodesToRepository()
        {
            if (_entryNode == null) return;

            var connectedTreeNodes = CollectConnectedTreeNodes(_entryNode);
            _presenter.SyncTreeNodes(_entryNode, connectedTreeNodes);
        }

        /// <summary> EntryNodeからポート接続を辿って到達可能なすべてのTreeNodeを収集する </summary>
        public static HashSet<TreeNode> CollectConnectedTreeNodes(BehaviourTree.EntryNode entryNode)
        {
            var connectedNodes = new HashSet<TreeNode>();
            if (entryNode == null) return connectedNodes;

            var queue = new Queue<TreeNode>();
            queue.Enqueue(entryNode);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var children = current.Children;
                if (children == null) continue;

                foreach (var child in children)
                {
                    if (child != null && !ReferenceEquals(child, entryNode) && connectedNodes.Add(child))
                    {
                        queue.Enqueue(child);
                    }
                }
            }

            return connectedNodes;
        }

        private bool TryAddBehaviourTreeNode(INode node)
        {
            if (node is IBehaviourTreeGraphNode behaviourTreeNode)
            {
                if (_behaviourTreeNodes.Contains(behaviourTreeNode)) return false;

                _behaviourTreeNodes.Add(behaviourTreeNode);
                return true;
            }

            return false;
        }

        private bool TryGetGraphChangedSaveNode(INode node)
        {
            if(node is GraphChangedSaveNode graphChangedSaveNode)
            {
                _changedSaveNode = graphChangedSaveNode;
                return true;
            }

            return false;
        }
    }

    public class BehaviourTreeGraphPresenter
    {
        public BehaviourTreeGraphPresenter(BehaviourTreeGraphView behaviourTreeGraphView)
        {
            _behaviourTreeGraphView = behaviourTreeGraphView;
            _behaviourTreeGraphView.OnChangedEntryNodes += HandleChangedEntryNodes;

            Init().Forget();
        }

        public async UniTask Init()
        {
            _bossAIBehaviourTreeNodeRepository =  await AssetsLoader.LoadAssetAsync<BossAIBehaviourTreeNodeRepositry>(AAGBossEnemyGroup.kAssets_Data_BossEnemy_Repositry_BossAIBehaviourTreeNodeRepositry);
            Debug.Log("Presenterの初期化が完了しました");
        }

        public void OnDisable()
        {
            AssetsLoader.Release(AAGBossEnemyGroup.kAssets_Data_BossEnemy_Repositry_BossAIBehaviourTreeNodeRepositry);
        }

        private bool _isSaveScheduled = false;

        public void SaveRepository()
        {
            ScriptableObject repositoryAsset = _bossAIBehaviourTreeNodeRepository as ScriptableObject;
            if (repositoryAsset == null) return;

            EditorUtility.SetDirty(repositoryAsset);

            if (_isSaveScheduled) return;
            _isSaveScheduled = true;

            EditorApplication.delayCall += () =>
            {
                _isSaveScheduled = false;
                if (repositoryAsset != null)
                {
                    AssetDatabase.SaveAssetIfDirty(repositoryAsset);
                }
            };
        }

        public void HandleChangedEntryNodes(BehaviourTree.EntryNode entryNode)
        {
            if (_bossAIBehaviourTreeNodeRepository == null)
            {
                Debug.Log("Presenterの初期化が終了していません");
                return;
            }

            _bossAIBehaviourTreeNodeRepository.SaveEntryNode(entryNode);
        }

        public void SyncTreeNodes(BehaviourTree.EntryNode entryNode, IEnumerable<TreeNode> connectedNodes)
        {
            if (_bossAIBehaviourTreeNodeRepository == null)
            {
                Debug.Log("Presenterの初期化が終了していません");
                return;
            }

            _bossAIBehaviourTreeNodeRepository.SyncTreeNodes(entryNode, connectedNodes);
        }

        private BehaviourTreeGraphView _behaviourTreeGraphView = null;

        private IBossAIBehaviourTreeNodeRepository _bossAIBehaviourTreeNodeRepository = null;
    }
    #endregion
}
#endif
