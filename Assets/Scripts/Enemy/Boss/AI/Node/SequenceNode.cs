
using BossEnemy.Character;
using System;

namespace BossEnemy.AI.BehaviourTree
{
    #region 子ノードをすべて順番に実行するSequenceNode
    /// <summary> 子ノードをすべて順番に実行する </summary>
    [Serializable]
    public class SequenceNode : BossCharacterBehaviourTreeNode
    {
        public override void Init(IBossCharacterEntity bossCharacterEntity, NodeRunningConditionNotifier nodeRunningEndNotifier)
        {
            base.Init(bossCharacterEntity, nodeRunningEndNotifier);

            foreach (var child in _childrenNode)
            {
                child.Init(_sequenceChildNodeRunningEndNotifier);
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
            ProceedSequence();

            _sequenceChildNodeRunningEndNotifier.OnRunningEnd += ProceedSequence;
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

            _sequenceChildNodeRunningEndNotifier.OnRunningEnd -= ProceedSequence;
        }

        private int _sequenceCount = 0;

        /// <summary> 現在実行中のノード </summary>
        private ITreeNode _currentNode = null;

        /// <summary> シーケンス内の子ノード専用Notifier </summary>
        private NodeRunningConditionNotifier _sequenceChildNodeRunningEndNotifier = new();

        private void EntryNextChildNode(ITreeNode nextNode)
        {
            if (nextNode == null) return;

            SearchNextRunningNode(nextNode);
        }

        private void ProceedSequence()
        {
            if (_sequenceCount >= _childrenNode.Length)
            {
                HandleRunningEnd();
                return;
            };

            EntryNextChildNode(_childrenNode[_sequenceCount]);
            _sequenceCount++;
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

            _currentNode.OnExit();
            _currentNode = nextNode;
            _currentNode.OnEnter();
        }
    }
    #endregion
}
