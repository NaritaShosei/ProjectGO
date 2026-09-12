
using BossEnemy.Character;
using System;
using UnityEngine;

namespace BossEnemy.AI.BehaviourTree
{
    #region 子ノードをすべて順番に実行するSequenceNode
    /// <summary> 子ノードをすべて順番に実行する </summary>
    [Serializable]
    public class SequenceNode : BossCharacterBehaviourTreeNode
    {
        public override void Init(IBossCharacterEntity bossCharacterEntity, NodeRunningConditionNotifier nodeRunningEndNotifier)
        {
            _sequenceChildNodeRunningEndNotifier = new();

            Init(nodeRunningEndNotifier);
            _bossCharacterEntity = bossCharacterEntity;

            // 子ノードを初期化
            if (_childrenNode != null && _childrenNode.Length > 0)
            {
                foreach (var child in _childrenNode)
                {
                    InitChildren(child, _sequenceChildNodeRunningEndNotifier);
                }
            }
        }

        public override NodeCondition TryEntry()
        {
            return NodeCondition.Success;
        }

        public override NodeCondition TryEntryNextNode(out ITreeNode nextNode)
        {
            nextNode = this;
            return NodeCondition.Running;
        }

        public override void OnEnter()
        {
            if(_currentNode != null) _currentNode = null;

            _sequenceCount = 0;

            _sequenceChildNodeRunningEndNotifier.OnResearchBehaviourTree += ProceedSequence;

            ProceedSequence();
        }

        public override void OnUpdate()
        {
            if (_currentNode == null) return;
            _currentNode.OnUpdate();
        }

        public override void OnExit()
        {
            if (_currentNode != null)
                _currentNode.OnExit();

            _currentNode = null;

            _sequenceChildNodeRunningEndNotifier.OnResearchBehaviourTree -= ProceedSequence;
        }

        private int _sequenceCount = 0;

        /// <summary> 現在実行中のノード </summary>
        private ITreeNode _currentNode = null;

        /// <summary> シーケンス内の子ノード専用Notifier </summary>
        private NodeRunningConditionNotifier _sequenceChildNodeRunningEndNotifier = new();

        /// <summary> Sequenceを進める </summary>
        private void ProceedSequence()
        {
            if (_sequenceCount >= _childrenNode.Length)
            {
                HandleRunningEnd();
                return;
            };

            ITreeNode nextRunningNode = _childrenNode[_sequenceCount];
            _sequenceCount++;
            EntryNextChildNode(nextRunningNode);
        }

        private void EntryNextChildNode(ITreeNode nextNode)
        {
            if (nextNode == null) return;

            SearchNextRunningNode(nextNode);
        }

        /// <summary> 次の行動を決める </summary>
        private void SearchNextRunningNode(ITreeNode searchNode)
        {
            if (searchNode == null) return;

            ITreeNode nextNode = searchNode;
            NodeCondition runningCondition = NodeCondition.Success;
            int count = 0;

            while (runningCondition != NodeCondition.Running)
            {
                runningCondition = nextNode.TryEntryNextNode(out nextNode);
                count++;

                if (runningCondition == NodeCondition.Failure)
                {
                    return;
                }
            }

            ChangeNode(nextNode);
        }

        /// <summary> 現在のNodeを変更する </summary>
        /// <param name="nextNode"> 次のNode </param>
        private void ChangeNode(ITreeNode nextNode)
        {
            if (nextNode == null) return;
            if(_currentNode != null) _currentNode.OnExit();

            _currentNode = nextNode;

            Debug.Log($"現在実行中のSequence : {_currentNode.GetType()}");

            _currentNode.OnEnter();
        }
    }
    #endregion
}
