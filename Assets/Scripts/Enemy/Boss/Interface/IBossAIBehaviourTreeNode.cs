using Boss.AI.BehaviourTree;
using Boss.Character;
using UnityEngine;

namespace Boss.Interface
{
    public interface IBossAIBehaviourTreeNode
    {
        /// <summary> 初期化 </summary>
        public void Init(IBossCharacterEntity bossCharacterEntity, NodeRunningConditionNotifier nodeRunningEndNotifier);
    }
}
