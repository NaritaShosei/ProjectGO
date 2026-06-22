using UnityEngine;

namespace BossEnemy.BehaviorTree
{
    /// <summary> Nodeへの遷移結果 </summary>
    public enum NodeCondition
    {
        Ready,
        Success,
        Failure,
        Running
    }

    #region TreeのNode遷移を行い操作するクラス
    /// <summary>
    /// BehaviorTreeの操作クラス
    /// </summary>
    public class BehaviorController
    {
        public BehaviorController(ITreeNode origin)
        {
            _originNode = origin;
        }

        /// <summary> Treeの探索を開始する </summary>
        public void StartSearch()
        {
            Debug.Log("探索開始");
            ChangeNode(_originNode);
        }

        /// <summary> Treeの探索を強制的に最初からやり直す </summary>
        public void ForceRestartSearch()
        {
            Debug.Log("探索強制再始動");
            ChangeNode(_originNode);
        }

        /// <summary> 毎フレーム実行する処理 </summary>
        public void OnUpdate()
        {
            if (_currentNode == null) return;

            _currentNode.OnUpdate();
        }

        /// <summary> 現在のNodeを変更する </summary>
        /// <param name="nextNode"> 次のNode </param>
        public void ChangeNode(ITreeNode nextNode)
        {
            if (nextNode == null) return;
            if (nextNode.Controller == null) nextNode.Init(this);

            if (_currentNode != null) 
                _currentNode.OnExit();

            _currentNode = nextNode;
            _currentNode.OnEnter();
        }

        /// <summary> 現在のNode </summary>
        private ITreeNode _currentNode = null;

        /// <summary> Entry地点のNode </summary>
        private readonly ITreeNode _originNode = null;
    }
    #endregion

    #region 各NodeのベースとなるClassとInterface
    /// <summary> TreeNodeのInterface </summary>
    public interface ITreeNode
    {
        /// <summary> BossEnemyを操るBehaviourTree </summary>
        BehaviorController Controller { get; }

        /// <summary> BehaviourTreeをSetする </summary>
        void Init(BehaviorController behaviourTree);

        /// <summary> このNodeへの遷移条件を確認して結果を返す </summary>
        NodeCondition TryEntry();

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
        public BehaviorController Controller => _behaviorController;

        public virtual void Init(BehaviorController behaviourController)
        {
            _behaviorController = behaviourController;
        }

        public abstract NodeCondition TryEntry();
        public virtual void OnEnter() { return; }
        public virtual void OnUpdate() { return; }
        public virtual void OnExit() { return; }

        private BehaviorController _behaviorController = null;
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
            foreach (var node in _childrenNode)
            {
                NodeCondition condition = node.TryEntry();

                switch (condition)
                {
                    case NodeCondition.Success:
                        Controller.ChangeNode(node);
                        break;
                    case NodeCondition.Failure:
                        continue;
                }

                return condition;
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
        }

        public override NodeCondition TryEntry()
        {
            return NodeCondition.Success;
        }

        public override void OnEnter()
        {
            if (_runningNode == null) return;

            _runningNode.OnEnter();
        }

        public override void OnUpdate()
        {
            if (_runningNode == null) return;

            _runningNode.OnUpdate();
        }

        public override void OnExit()
        {
            if (_runningNode == null) return;

            _runningNode.OnExit();
        }

        /// <summary> 子ノード </summary>
        private ITreeNode[] _childrenNode = null;

        /// <summary> 現在実行中のノード </summary>
        private ITreeNode _runningNode = null;
    }
    #endregion
}
