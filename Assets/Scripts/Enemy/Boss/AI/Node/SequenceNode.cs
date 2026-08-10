using BossEnemy.Character;

namespace BossEnemy.AI
{
    #region 子ノードをすべて順番に実行するSequenceNode
    /// <summary> 子ノードをすべて順番に実行する </summary>
    public class SequenceNode : TreeNodeBase
    {
        public SequenceNode(ITreeNode[] childrenNode)
        {
            _childrenNode = childrenNode;

            _sequenceChildNodeRunningEndNotifier.OnRunningEnd += ProceedSequence;
        }

        public override void Init(RunningConditionNotifier nodeRunningEndNotifier)
        {
            base.Init(nodeRunningEndNotifier);

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

        public void EntryNextChildNode(ITreeNode nextNode)
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

        /// <summary> シーケンス内の子ノード専用Notifier </summary>
        private RunningConditionNotifier _sequenceChildNodeRunningEndNotifier = new();

        private void ProceedSequence()
        {
            if (_sequenceCount >= _childrenNode.Length)
            {
                RunningEnd();
                return;
            };

            EntryNextChildNode(_childrenNode[_sequenceCount]);
        }
    }
    #endregion
}
