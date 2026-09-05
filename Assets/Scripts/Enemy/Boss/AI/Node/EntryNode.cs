using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace BossEnemy.AI.BehaviourTree
{
    [Serializable]
    public class EntryNode : BossCharacterBehaviourTreeNode
    {
        public int PlayableCharactersID => _playableCharactersID;

        public override NodeCondition TryEntry()
        {
            return NodeCondition.Success;
        }

        public override NodeCondition TryEntryNextNode(out ITreeNode nextNode)
        {
            if (_childrenNode == null)
            {
                Debug.LogError("子ノードが設定されていません");
                nextNode = null;
                return NodeCondition.Failure;
            }

            nextNode = _childrenNode[0];
            return NodeCondition.Success;
        }

        /// <summary> 操作対象となるキャラクターのIDを設定する </summary>
        /// <param name="id">CharacterID</param>
        public void SetPlayableCharacterID(int id)
        {
            _playableCharactersID = id;
        }

        /// <summary> 操作対象のキャラクターのID </summary>
        [SerializeField] private int _playableCharactersID;
    }
}
