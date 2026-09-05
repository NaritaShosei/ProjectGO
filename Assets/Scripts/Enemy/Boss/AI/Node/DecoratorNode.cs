using BossEnemy.Enum;
using System;
using UniRx;
using UnityEngine;

namespace BossEnemy.AI.BehaviourTree
{
    /// <summary> 通ったら子Nodeを実行して、通らなければFailureを返すNode </summary>
    [Serializable]
    public abstract class DecoratorNode : BossCharacterBehaviourTreeNode
    {
        public DecoratorNode()
        {
            int childrenLength = 1;
            _childrenNode = new TreeNode[childrenLength];
        }

        public override NodeCondition TryEntryNextNode(out ITreeNode nextNode)
        {
            int topPriorityChildIndex = 0;

            if (_childrenNode == null)
            {
                Debug.LogError("子ノードがNullです");
                nextNode = null;
                return NodeCondition.Failure;
            }

            NodeCondition condition = _childrenNode[topPriorityChildIndex].TryEntry();

            if (condition == NodeCondition.Running || condition == NodeCondition.Success)
            {
                nextNode = _childrenNode[topPriorityChildIndex];
                return NodeCondition.Success;
            }

            nextNode = null;
            return NodeCondition.Failure;
        }
    }

    [Serializable]
    public class ArmorBrokenDecoratorNode : DecoratorNode
    {
        public void SetCanEntryCondition(ArmorAttachmentType[] canEntryArmorConditions)
        {
            _canEntryArmorConditions = canEntryArmorConditions;
        }

        public override NodeCondition TryEntry()
        {
            foreach (var condition in _canEntryArmorConditions)
            {
                if (!_bossCharacterEntity.GetArmorStats(condition).IsArmorBroken)
                {
                    return NodeCondition.Failure;
                }
            }

            return NodeCondition.Success;
        }

        [SerializeField] private ArmorAttachmentType[] _canEntryArmorConditions = null;
    }

    [Serializable]
    public class BossCharacterPostureDecoratorNode : DecoratorNode
    {
        /// <summary> Entry可能になる現在の姿勢を設定する </summary>
        public void SetCanEntryCondition(PostureType postureType)
        {
            _canEntryPosture = postureType;
        }

        public override NodeCondition TryEntry()
        {
            if(_canEntryPosture == _bossCharacterEntity.CurrentCharacterPostureType.Value)
            {
                return NodeCondition.Success;
            }

            return NodeCondition.Failure;
        }

        [SerializeField] private PostureType _canEntryPosture;
    }

    [Serializable]
    public class TargetDistanceDecoratorNode : DecoratorNode
    {
        public void SetCanEntryCondition(float distance)
        {
            _canEntryDistance = distance;
        }

        /// <summary> 現在のターゲットとの距離が_canEntryDistanceより近ければエントリ－可能となる </summary>
        public override NodeCondition TryEntry()
        {
            var toTargetDistance =
                Vector3.Distance(
                    _bossCharacterEntity.Position.Value,
                    _bossCharacterEntity.AttackTarget.GetTargetCenter().position);

            if (toTargetDistance < _canEntryDistance)
            {
                return NodeCondition.Success;
            }

            return NodeCondition.Failure;
        }

        [SerializeField] private float _canEntryDistance;
    }

    [Serializable]
    public class CurrentPhaseDecoratorNode : DecoratorNode
    {
        public void SetCanEntryCondition(int canEntryPhase)
        {
            _canEntryPhase = canEntryPhase;
        }

        public override NodeCondition TryEntry()
        {
            if (_bossCharacterEntity.CharacterCurrentStats.PhaseNum == _canEntryPhase)
            {
                return NodeCondition.Success;
            }

            return NodeCondition.Failure;
        }

        [SerializeField] private int _canEntryPhase;
    }

    [Serializable]
    public class CharacterHPDecoratorNode : DecoratorNode
    {
        public void SetCanEntryCondition(InequalityType numericalComparisonType, int canEntryRemainingHP)
        {
            _numericalComparisonType = numericalComparisonType;
            _canEntryRemainingHP = canEntryRemainingHP;
        }

        public override NodeCondition TryEntry()
        {
            switch (_numericalComparisonType)
            {
                case InequalityType.Greater:
                    if(_bossCharacterEntity.CurrentHP.Value < _canEntryRemainingHP)
                        return NodeCondition.Success;
                    break;
                case InequalityType.Less:
                    if(_bossCharacterEntity.CurrentHP.Value > _canEntryRemainingHP)
                        return NodeCondition.Success;
                    break;
                case InequalityType.Equals:
                    if (_bossCharacterEntity.CurrentHP.Value == _canEntryRemainingHP)
                        return NodeCondition.Success;
                    break;
            }

            return NodeCondition.Failure;
        }

        [SerializeField] private InequalityType _numericalComparisonType;

        [SerializeField] private int _canEntryRemainingHP;
    }

    [Serializable]
    public class CountDownDecoratorNode : DecoratorNode
    {
        public void SetCanEntryCondition(int canEntryCount)
        {
            _canEntryCount = canEntryCount;
        }

        public override NodeCondition TryEntry()
        {
            if(_canEntryCount <= _currentCount)
            {
                _currentCount = 0;
                return NodeCondition.Success;
            }

            _currentCount++;
            return NodeCondition.Failure;
        }

        [SerializeField] private int _canEntryCount;

        private int _currentCount = 0;
    }
}
