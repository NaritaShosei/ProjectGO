using Cysharp.Threading.Tasks;
using System;
using UniRx;
using UnityEngine;

namespace BossEnemy.BehaviorTree
{
    /// <summary> Nodeへの遷移結果 </summary>
    public enum NodeCondition
    {
        Success,
        Failure,
        Running
    }

    /// <summary> BehaviorTreeのもととなるインターフェース </summary>
    public interface IBehaviorTree
    {
        
    }


    #region 実行中の実行終了時に実行終了を通知するクラス
    public class NodeRunningEndNotifier
    {
        public event Action OnRunningEnd;

        public void RunningEnd()
        {
            OnRunningEnd?.Invoke();
        }
    }
    #endregion

    #region TreeのNode遷移を行い操作するクラス

    /// <summary>
    /// BehaviorTreeの操作クラス
    /// </summary>
    public class BehaviorController
    {
        public BehaviorController(ITreeNode origin, NodeRunningEndNotifier runningEndNotifier)
        {
            _originNode = origin;
            _nodeRunningEndNotifier = runningEndNotifier;
            _nodeRunningEndNotifier.OnRunningEnd += OnRunning;
        }

        /// <summary> 毎フレーム実行する処理 </summary>
        public void OnUpdate()
        {
            if (_currentNode == null) return;

            _currentNode.OnUpdate();
        }

        /// <summary> 次の行動を決める </summary>
        public void OnRunning()
        {
            if(_originNode == null) return;

            if(_currentNode != null)
                _currentNode.OnExit();

            _currentNode = _originNode;
            NodeCondition runningCondition = NodeCondition.Success;

            int count = 0;
            while (runningCondition != NodeCondition.Running)
            {
                runningCondition = _currentNode.TryGetNextNode(ref _currentNode);
                count++;

                Debug.Log("ランニング：" + _currentNode.GetType().Name);

                if (runningCondition == NodeCondition.Failure)
                {
                    Debug.LogError("行動の選択を失敗しました");
                    Debug.LogError("現在のノード：" + _currentNode.GetType().Name);
                    Debug.LogError("階層段階：" + count);
                    return;
                }
            }

            ChangeNode(_currentNode);
        }

        public void StopRunning()
        {
            if (_originNode == null) return;

            if (_currentNode != null)
                _currentNode.OnExit();
        }

        /// <summary> 現在のNodeを変更する </summary>
        /// <param name="nextNode"> 次のNode </param>
        public void ChangeNode(ITreeNode nextNode)
        {
            if (nextNode == null) return;
            if (!nextNode.IsInit) nextNode.Init(this, _nodeRunningEndNotifier);

            _disposables.Clear();

            _currentNode = nextNode;
            _currentNode.OnEnter();
        }

        /// <summary> 現在のNode </summary>
        private ITreeNode _currentNode = null;

        /// <summary> Entry地点のNode </summary>
        private readonly ITreeNode _originNode = null;

        private readonly NodeRunningEndNotifier _nodeRunningEndNotifier;

        private CompositeDisposable _disposables = new CompositeDisposable();
    }
    #endregion

    #region 各NodeのベースとなるClassとInterface
    /// <summary> TreeNodeのInterface </summary>
    public interface ITreeNode
    {
        /// <summary> BossEnemyを操るBehaviourTree </summary>
        BehaviorController Controller { get; }

        /// <summary> 初期化済み判定フラグ </summary>
        public bool IsInit { get; }

        /// <summary> BehaviourTreeをSetする </summary>
        void Init(BehaviorController behaviourController, NodeRunningEndNotifier nodeRunningEndNotifier);

        /// <summary> このNodeへの遷移条件を確認して結果を返す </summary>
        NodeCondition TryEntry();

        /// <summary> 子ノードから遷移可能なノードを選出して渡す </summary>
        /// <param name="nextNode"> 次のNode </param>
        /// <returns> 
        /// 次のNodeへの遷移結果フラグ
        /// このフラグがFalseなら現在のNodeをゴールとする
        /// </returns>
        NodeCondition TryGetNextNode(ref ITreeNode nextNode);

        /// <summary> このNodeへの遷移が成功した際の処理 </summary>
        void OnEnter();

        /// <summary> このNodeの実行中の処理 </summary>
        void OnUpdate();

        /// <summary> このNodeを離れる際の処理 </summary>
        void OnExit();
    }

    /// <summary> BehaviorTreeのNodeの基底クラス </summary>
    public abstract class TreeNodeBase : ITreeNode
    {
        public bool IsInit => _isInit;

        public BehaviorController Controller => _behaviorController;

        public virtual void Init(BehaviorController behaviourController, NodeRunningEndNotifier nodeRunningEndNotifier)
        {
            _isInit = true;
            _behaviorController = behaviourController;
            _nodeRunningEndNotifier = nodeRunningEndNotifier;
        }

        public abstract NodeCondition TryEntry();
        public abstract NodeCondition TryGetNextNode(ref ITreeNode nextNode);
        public virtual void OnEnter() { return; }
        public virtual void OnUpdate() { return; }
        public virtual void OnExit() { return; }

        private NodeRunningEndNotifier _nodeRunningEndNotifier = null;
        private BehaviorController _behaviorController = null;

        private bool _isInit = false;

        protected void RunningEnd() => _nodeRunningEndNotifier.RunningEnd();
    }
    #endregion

    #region 子ノードを順番に実行して一番最初にSuccessになったNodeを実行するSelectorNode
    /// <summary> 子ノードを順番に実行して一番最初にSuccessになったNodeを実行する </summary>
    public class SelectorNode : TreeNodeBase
    {
        public SelectorNode(ITreeNode[] childrenNode)
        {
            _childrenNode = childrenNode;
        }

        public override NodeCondition TryEntry()
        {
            return NodeCondition.Success;
        }
        
        public override NodeCondition TryGetNextNode(ref ITreeNode nextNode)
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

    #region 子ノードをすべて順番に実行するSequenceNode
    /// <summary> 子ノードをすべて順番に実行する </summary>
    public class SequenceNode : TreeNodeBase
    {
        public SequenceNode(ITreeNode[] childrenNode)
        {
            _childrenNode = childrenNode;

            _sequenceChildNodeRunningEndNotifier.OnRunningEnd += ProceedSequence;
        }

        public override void Init(BehaviorController behaviourController, NodeRunningEndNotifier nodeRunningEndNotifier)
        {
            base.Init(behaviourController, nodeRunningEndNotifier);

            foreach (var child in _childrenNode)
            {
                child.Init(Controller, _sequenceChildNodeRunningEndNotifier);
            }
        }

        public override NodeCondition TryEntry()
        {
            return NodeCondition.Success;
        }

        public override NodeCondition TryGetNextNode(ref ITreeNode nextNode)
        {
            return NodeCondition.Running;
        }

        public override void OnEnter()
        {
            ProceedSequence();
        }

        public override void OnUpdate()
        {
            if (_currentNode == null) return;
            _currentNode.OnUpdate();
        }

        public override void OnExit()
        {
            _sequenceCount = 0;

            if (_currentNode != null)
                _currentNode.OnExit();

            _currentNode = null;
        }

        public void OnEnterNextChildNode(ITreeNode nextNode)
        {
            if (nextNode == null) return;

            if (_currentNode != null)
            {
                _currentNode.OnExit();
            }

            _sequenceCount++;

            _currentNode = nextNode;
            _currentNode.OnEnter();

        }

        private int _sequenceCount = 0;

        /// <summary> 子ノード </summary>
        private ITreeNode[] _childrenNode = null;

        /// <summary> 現在実行中のノード </summary>
        private ITreeNode _currentNode = null;

        /// <summary> シーケンス内のノード専用Notifier </summary>
        private NodeRunningEndNotifier _sequenceChildNodeRunningEndNotifier = new();

        private void ProceedSequence()
        {
            if (_sequenceCount >= _childrenNode.Length)
            {
                RunningEnd();
                return;
            };

            OnEnterNextChildNode(_childrenNode[_sequenceCount]);
        }
    }
    #endregion
}
