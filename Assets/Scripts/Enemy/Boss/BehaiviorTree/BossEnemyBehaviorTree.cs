using BossEnemy.Data;
using System;
using UnityEngine;

namespace BossEnemy.BehaviorTree
{
    /// <summary> Nodeへの遷移結果 </summary>
    public enum NodeCondition
    {
        Ready,
        Success,
        Failure
    }

    /// <summary> BossEnemyAIBehaviorTree </summary>
    [Serializable]
    public class BossEnemyBehaviorTree
    {
        public BossEnemyBehaviorTree(
            EnemyServices services,
            BossEnemyData bossEnemyData)
        {
            _services = services;
            _enemyData = bossEnemyData;
            _originNode = bossEnemyData.OriginNode;
        }

        public EnemyServices Services => _services;

        public BossEnemyData BossEnemyData => _enemyData;

        /// <summary> Treeの探索を開始する </summary>
        public void StartSearchNode()
        {
            Debug.Log("探索開始");
            ChangeNode(_originNode);
        }

        /// <summary> 現在のNodeを変更する </summary>
        /// <param name="nextNode"> 次のNode </param>
        public void ChangeNode(ITreeNode nextNode)
        {
            if (nextNode == null) return;
            if (nextNode.BehaviourTree == null) nextNode.Init(this);

            if (_currentNode != null) 
                _currentNode.OnExit();

            _currentNode = nextNode;
            _currentNode.OnEnter();
        }

        /// <summary> 毎フレーム実行する処理 </summary>
        public void OnUpdate()
        {
            if (_currentNode == null) return;

            _currentNode.OnUpdate();
        }

        /// <summary> OriginNodeを置き換える </summary>
        public void ChangeOriginNode(ITreeNode originNode) => _originNode = originNode;

        /// <summary> 現在のNode </summary>
        private ITreeNode _currentNode = null;

        /// <summary> Entry地点のNode </summary>
        private ITreeNode _originNode = null;

        /// <summary> Enemyが取得できるサービス </summary>
        private EnemyServices _services;

        /// <summary> 操作するEnemyのデータクラス </summary>
        private BossEnemyData _enemyData;
    }

    /// <summary> TreeNodeのInterface </summary>
    public interface ITreeNode
    {
        /// <summary> BossEnemyを操るBehaviourTree </summary>
        BossEnemyBehaviorTree BehaviourTree { get; }

        /// <summary> BehaviourTreeをSetする </summary>
        void Init(BossEnemyBehaviorTree behaviourTree);

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
    public abstract class TreeNode : ITreeNode
    {
        public BossEnemyBehaviorTree BehaviourTree => _behaviorTree;

        public virtual void Init(BossEnemyBehaviorTree behaviourTree)
        {
            _behaviorTree = behaviourTree;
        }

        public abstract NodeCondition TryEntry();
        public virtual void OnEnter() { return; }
        public virtual void OnUpdate() { return; }
        public virtual void OnExit() { return; }

        private BossEnemyBehaviorTree _behaviorTree = null;
    }

    /// <summary> 行動の実行を行うNode(最終的なTree構造の最深部) </summary>
    public abstract class ActionNode : TreeNode
    {
        
    }

    /// <summary> 通ったら子Nodeを実行して、通らなければFailureを返すNode </summary>
    public abstract class DecoratorNode : TreeNode
    {
        [Header("子Node")]
        [SerializeField, Tooltip("子Node")]
        protected TreeNode _childNode = null;
    }

    /// <summary> 子ノードを順番に実行して一番最初にSuccessになったNodeを実行する </summary>
    public class SelectorNode : TreeNode
    {
        public bool IsEndNode
        {
            // 子NodeがいなければこのNodeが終点となる
            get
            {
                if(_childrenNode == null) return true;
                else if(_childrenNode.Length == 0) return true;
                else return false;
            }
        }

        public override NodeCondition TryEntry()
        {
            foreach (var node in _childrenNode)
            {
                NodeCondition condition = node.TryEntry();

                switch (condition)
                {
                    case NodeCondition.Success:
                        BehaviourTree.ChangeNode(node);
                        break;
                    case NodeCondition.Failure:
                        continue;
                }

                return condition;
            }

            return NodeCondition.Failure;
        }

        [Header("子Node")]
        [SerializeField, Tooltip("子Node")]
        private TreeNode[] _childrenNode = null;
    }

    /// <summary> 子ノードをすべて順番に実行する </summary>
    public class SequenceNode : TreeNode
    {
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

        [Header("子Node")]
        [SerializeField, Tooltip("子Node")]
        private TreeNode[] _childrenNode = null;

        /// <summary> 現在実行中のノード </summary>
        private ITreeNode _runningNode = null;
    }

}
