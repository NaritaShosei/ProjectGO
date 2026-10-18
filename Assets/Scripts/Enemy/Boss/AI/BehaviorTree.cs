using Boss.Character;
using System;
using UniRx;
using Unity.VisualScripting;
using UnityEngine;

namespace Boss.AI.BehaviourTree
{
    /// <summary> Nodeへの遷移結果 </summary>
    public enum NodeCondition
    {
        Success,
        Failure,
        Running
    }


    #region ビヘイビアツリーの実行状況通知クラス
    public class NodeRunningConditionNotifier
    {
        /// <summary> ビヘイビアツリーを再走し次の行動を探すイベント </summary>
        public event Action OnResearchBehaviourTree;

        /// <summary> 非同期処理を行ってる行動のCancelを行うイベント </summary>
        public event Action OnCancelAsyncAction;

        public void HandleResearchBehaviourTree()
        {
            OnResearchBehaviourTree?.Invoke();
        }

        public void HandleCancelAsyncAction()
        {
            // 同一通知の処理中に戻ってきた呼び出しは、既に伝播済みなので無視する。
            if (_isHandlingCancelAsyncAction) return;

            // SequenceNodeなどのノードで親子のNotifierを相互に接続しているため、
            // キャンセル通知が親子間を往復して再帰しないようにする。
            _isHandlingCancelAsyncAction = true;
            try
            {
                OnCancelAsyncAction?.Invoke();
            }
            finally
            {
                _isHandlingCancelAsyncAction = false;
            }
        }

        private bool _isHandlingCancelAsyncAction;
    }
    #endregion

    #region TreeのNode遷移を行い操作するクラス

    /// <summary>
    /// BehaviourTreeの操作クラス
    /// </summary>
    public class BehaviourController : IDisposable
    {
        public BehaviourController(TreeNode origin)
        {
            _originNode = origin;
            _nodeRunningEndNotifier = origin.NodeRunningConditionNotifier;
            _nodeRunningEndNotifier.OnResearchBehaviourTree += SearchNextRunningNode;
        }

        public void Dispose()
        {
            _originNode.Dispose();
        }

        /// <summary> 毎フレーム実行する処理 </summary>
        public void OnUpdate()
        {
            if (_currentNode == null) return;

            _currentNode.OnUpdate();
        }

        /// <summary> 次の行動を決める </summary>
        public void SearchNextRunningNode()
        {
            if(_originNode == null) return;

            TreeNode nextNode = _originNode;
            NodeCondition runningCondition = NodeCondition.Success;
            int count = 0;

            while (runningCondition != NodeCondition.Running)
            {
                var currentNode = nextNode;
                runningCondition = currentNode.TryEntryNextNode(out nextNode);
                count++;


                if (runningCondition == NodeCondition.Failure || nextNode == null)
                {
                    Debug.Log("行動の切り替えに失敗しました、現在の行動を続行します。");
                    return;
                }
                else Debug.Log($"現在探索中のノード：{ currentNode.GetType() }");
            }

            ChangeNode(nextNode);
        }

        /// <summary> 現在の行動を強制停止させる </summary>
        public void StopRunning()
        {
            if (_originNode == null) return;

            if (_currentNode != null)
                _currentNode.OnExit();
        }

        /// <summary> 現在のNode </summary>
        private TreeNode _currentNode = null;

        /// <summary> Entry地点のNode </summary>
        private readonly TreeNode _originNode = null;

        private readonly NodeRunningConditionNotifier _nodeRunningEndNotifier;

        /// <summary> 現在のNodeを変更する </summary>
        /// <param name="nextNode"> 次のNode </param>
        private void ChangeNode(TreeNode nextNode)
        {
            if (nextNode == null) return;
            if(_currentNode != null) _currentNode.OnExit();
            
            _currentNode = nextNode;

            Debug.Log($"現在実行中のノード：{_currentNode.GetType()}");

            _currentNode.OnEnter();
        }
    }
    #endregion

    #region 各NodeのベースとなるClass
    /// <summary> BehaviorTreeのノードクラス </summary>
    [Serializable]
    public class TreeNode : IPrototype<TreeNode>
    {
        /// <summary> 実行優先度(0に近いほど優先度：高) </summary>
        public uint RunningPriority => _runningPriority;

        public NodeRunningConditionNotifier NodeRunningConditionNotifier => _nodeRunningConditionNotifier;

        public void Init(NodeRunningConditionNotifier nodeRunningEndNotifier)
        {
            _isInit = true;
            _nodeRunningConditionNotifier = nodeRunningEndNotifier;
        }

        public virtual void Dispose()
        {
            _isInit = false;
            _nodeRunningConditionNotifier = null;
        }

        public void SetRunningPriority(uint priority) => _runningPriority = priority;

        public void SetChildren(TreeNode[] childrenNode) => _childrenNode = childrenNode;

        public TreeNode[] Children => _childrenNode;

        public virtual NodeCondition TryEntry()
        {
            return NodeCondition.Failure;
        }

        public virtual NodeCondition TryEntryNextNode(out TreeNode nextNode)
        {
            nextNode = null;
            return NodeCondition.Failure;
        }

        public virtual TreeNode Clone()
        {
            return null;
        }

        public virtual void OnEnter() { return; }
        public virtual void OnUpdate() { return; }
        public virtual void OnExit() { return; }

        private bool _isInit = false;

        protected NodeRunningConditionNotifier _nodeRunningConditionNotifier = null;

        [SerializeReference] protected TreeNode[] _childrenNode = null;

        [SerializeField] private uint _runningPriority = 0;

        protected void HandleRunningEnd()
        {
            Debug.Log("EndRunning");
            _nodeRunningConditionNotifier.HandleResearchBehaviourTree();
        }
    }
    #endregion
}
