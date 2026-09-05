using BossEnemy.Character;
using System;
using UniRx;
using UnityEngine;

namespace BossEnemy.AI.BehaviourTree
{
    /// <summary> Nodeへの遷移結果 </summary>
    public enum NodeCondition
    {
        Success,
        Failure,
        Running
    }


    #region 実行中の実行終了時に実行終了を通知するクラス
    public class NodeRunningConditionNotifier
    {
        public event Action OnRunningEnd;

        public void HandleRunningEnd()
        {
            OnRunningEnd?.Invoke();
        }
    }
    #endregion

    #region TreeのNode遷移を行い操作するクラス

    /// <summary>
    /// BehaviourTreeの操作クラス
    /// </summary>
    public class BehaviourController
    {
        public BehaviourController(ITreeNode origin)
        {
            _originNode = origin;
            _nodeRunningEndNotifier = origin.NodeRunningEndNotifier;
            _nodeRunningEndNotifier.OnRunningEnd += SearchNextRunningNode;
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

            ITreeNode nextNode = _originNode;
            NodeCondition runningCondition = NodeCondition.Success;
            int count = 0;

            while (runningCondition != NodeCondition.Running)
            {
                var currentNode = nextNode;
                runningCondition = currentNode.TryEntryNextNode(out nextNode);
                count++;

                if (runningCondition == NodeCondition.Failure)
                {
                    Debug.Log("行動の切り替えに失敗しました、現在の行動を続行します。");
                    return;
                }
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
        private ITreeNode _currentNode = null;

        /// <summary> Entry地点のNode </summary>
        private readonly ITreeNode _originNode = null;

        private readonly NodeRunningConditionNotifier _nodeRunningEndNotifier;

        /// <summary> 現在のNodeを変更する </summary>
        /// <param name="nextNode"> 次のNode </param>
        private void ChangeNode(ITreeNode nextNode)
        {
            if (nextNode == null) return;
            if(_currentNode != null) _currentNode.OnExit();
            
            _currentNode = nextNode;
            _currentNode.OnEnter();
        }
    }
    #endregion

    #region 各NodeのベースとなるClassとInterface
    /// <summary> TreeNodeのInterface </summary>
    public interface ITreeNode
    {
        /// <summary> 初期化済み判定フラグ </summary>
        public bool IsInit { get; }

        /// <summary> 実行優先度 </summary>
        public int RunningPriority { get; }

        /// <summary> ノードの実行状況通知クラス </summary>
        public NodeRunningConditionNotifier NodeRunningEndNotifier { get; }

        /// <summary> BehaviourTreeをSetする </summary>
        void Init(NodeRunningConditionNotifier nodeRunningEndNotifier);

        /// <summary> このNodeへの遷移条件を確認して結果を返す </summary>
        NodeCondition TryEntry();

        /// <summary> 子ノードから遷移可能なノードを選出して渡す </summary>
        /// <param name="nextNode"> 次のNode </param>
        /// <returns> 
        /// 次のNodeへの遷移結果フラグ
        /// このフラグがFalseなら現在のNodeをゴールとする
        /// </returns>
        NodeCondition TryEntryNextNode(out ITreeNode nextNode);

        /// <summary> このNodeへの遷移が成功した際の処理 </summary>
        void OnEnter();

        /// <summary> このNodeの実行中の処理 </summary>
        void OnUpdate();

        /// <summary> このNodeを離れる際の処理 </summary>
        void OnExit();
    }

    /// <summary> BehaviorTreeのNodeの基底クラス </summary>
    [Serializable]
    public class TreeNode : ITreeNode
    {
        public bool IsInit => _isInit;

        public int RunningPriority => _runningPriority;

        public NodeRunningConditionNotifier NodeRunningEndNotifier => _nodeRunningConditionNotifier;

        public void Init(NodeRunningConditionNotifier nodeRunningEndNotifier)
        {
            _isInit = true;
            _nodeRunningConditionNotifier = nodeRunningEndNotifier;
        }

        public void SetRunningPriority(int priority) => _runningPriority = priority;

        public void SetChildren(TreeNode[] childrenNode) => _childrenNode = childrenNode;

        public TreeNode[] Children => _childrenNode;

        public virtual NodeCondition TryEntry()
        {
            return NodeCondition.Failure;
        }

        public virtual NodeCondition TryEntryNextNode(out ITreeNode nextNode)
        {
            nextNode = null;
            return NodeCondition.Failure;
        }

        public virtual void OnEnter() { return; }
        public virtual void OnUpdate() { return; }
        public virtual void OnExit() { return; }

        private bool _isInit = false;

        protected NodeRunningConditionNotifier _nodeRunningConditionNotifier = null;

        [SerializeReference] protected TreeNode[] _childrenNode = null;

        [SerializeField] private int _runningPriority = 0;

        protected void HandleRunningEnd() => _nodeRunningConditionNotifier.HandleRunningEnd();
    }
    #endregion
}
