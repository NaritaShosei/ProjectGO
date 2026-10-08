using Boss.Character;
using Boss.Enum;
using System;
using UniRx;
using UnityEngine;

namespace Boss.AI.BehaviourTree
{
    /// <summary> 条件判定を行い、通過した際に子ノードを実行する機能装飾ノードの基底クラス </summary>
    #region DecoratorNode
    [Serializable]
    public abstract class DecoratorNode<TDecoratorNode> 
        : BossCharacterAIBehaviourTreeNode<TDecoratorNode> 
        where TDecoratorNode : DecoratorNode<TDecoratorNode>
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
    #endregion

    /// <summary> 初回行動が未開始の場合にエントリーを許可し行動を開始させる機能装飾ノード </summary>
    #region NotBeginActionDecoratorNode
    [Serializable]
    public class NotBeginActionDecoratorNode : DecoratorNode<NotBeginActionDecoratorNode>
    {
        public override NodeCondition TryEntry()
        {
            // もし最初の行動開始を開始していなければこのNode以降にある最初の行動を開始する
            if (!_bossCharacterEntity.IsBeganAction)
            {
                _bossCharacterEntity.BeginAction();
                return NodeCondition.Success;
            }

            return NodeCondition.Failure;
        }

        public override NotBeginActionDecoratorNode Clone()
        {
            var clone = new NotBeginActionDecoratorNode();
            return clone;
        }
    }
    #endregion

    /// <summary> 現在のボスの行動状態が指定条件と一致する場合にエントリーを許可する機能装飾ノード </summary>
    #region CurrentCharacterActionDecoratorNode
    [Serializable]
    public class CurrentCharacterActionDecoratorNode : DecoratorNode<CurrentCharacterActionDecoratorNode>
    {
        public void SetCanEntryCondition(CharacterAction canEntryArmorConditions)
        {
            _canEntryCharacterAction = canEntryArmorConditions;
        }

        public override NodeCondition TryEntry()
        {
            if(_bossCharacterEntity.CurrentAction.Value == _canEntryCharacterAction)
            {
                return NodeCondition.Success;
            }

            return NodeCondition.Failure;
        }

        public override CurrentCharacterActionDecoratorNode Clone()
        {
            var clone = new CurrentCharacterActionDecoratorNode();
            clone.SetCanEntryCondition(_canEntryCharacterAction);
            return clone;
        }

        [SerializeField] private CharacterAction _canEntryCharacterAction;
    }
    #endregion

    /// <summary> 鎧が破損中である場合にエントリーを許可する機能装飾ノード </summary>
    #region BreakingArmorDecoratorNode
    [Serializable]
    public class BreakingArmorDecoratorNode : DecoratorNode<BreakingArmorDecoratorNode>
    {
        public override NodeCondition TryEntry()
        {
            if(_bossCharacterEntity.BreakingArmorAttachmentType.Value != ArmorAttachmentType.None)
            {
                return NodeCondition.Success;
            }

            return NodeCondition.Failure;
        }

        public override BreakingArmorDecoratorNode Clone()
        {
            var clone = new BreakingArmorDecoratorNode();
            return clone;
        }
    }
    #endregion

    /// <summary> 指定した鎧部位がすべて破壊されている場合にエントリーを許可する機能装飾ノード </summary>
    #region ArmorBrokenDecoratorNode
    [Serializable]
    public class ArmorBrokenDecoratorNode : DecoratorNode<ArmorBrokenDecoratorNode>
    {
        public void SetCanEntryCondition(ArmorAttachmentType[] canEntryArmorConditions)
        {
            _canEntryArmorConditions = canEntryArmorConditions;
        }

        public override NodeCondition TryEntry()
        {
            if (_canEntryArmorConditions == null || _canEntryArmorConditions.Length == 0)
            {
                return NodeCondition.Failure;
            }

            bool hasArmorCondition = false;
            foreach (var condition in _canEntryArmorConditions)
            {
                // None は「装備なし」を示す既定値であり、鎧の破壊条件には使用できない。
                // 可変長の条件配列に残った既定値を鎧として問い合わせないよう除外する。
                if (condition == ArmorAttachmentType.None) continue;

                hasArmorCondition = true;
                if (!_bossCharacterEntity.GetArmorStats(condition).IsArmorBroken)
                {
                    return NodeCondition.Failure;
                }
            }

            return hasArmorCondition ? NodeCondition.Success : NodeCondition.Failure;
        }

        public override ArmorBrokenDecoratorNode Clone()
        {
            var clone = new ArmorBrokenDecoratorNode();
            clone.SetCanEntryCondition(_canEntryArmorConditions);
            return clone;
        }

        [SerializeField] private ArmorAttachmentType[] _canEntryArmorConditions = null;
    }
    #endregion

    /// <summary> 現在のボスの姿勢が指定条件と一致する場合にエントリーを許可する機能装飾ノード </summary>
    #region BossCharacterPostureDecoratorNode
    [Serializable]
    public class BossCharacterPostureDecoratorNode : DecoratorNode<BossCharacterPostureDecoratorNode>
    {
        /// <summary> Entry可能になる現在の姿勢を設定する </summary>
        public void SetCanEntryCondition(PostureType postureType)
        {
            _canEntryPosture = postureType;
        }

        public override NodeCondition TryEntry()
        {
            if(_canEntryPosture == _bossCharacterEntity.CurrentCharacterPostureType)
            {
                return NodeCondition.Success;
            }

            return NodeCondition.Failure;
        }

        public override BossCharacterPostureDecoratorNode Clone()
        {
            var clone = new BossCharacterPostureDecoratorNode();
            clone.SetCanEntryCondition(_canEntryPosture);
            return clone;
        }

        [SerializeField] private PostureType _canEntryPosture;
    }
    #endregion

    /// <summary> ターゲットとの距離が指定距離未満の場合にエントリーを許可する機能装飾ノード </summary>
    #region TargetDistanceDecoratorNode
    [Serializable]
    public class TargetDistanceDecoratorNode : DecoratorNode<TargetDistanceDecoratorNode>
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

        public override TargetDistanceDecoratorNode Clone()
        {
            var clone = new TargetDistanceDecoratorNode();
            clone.SetCanEntryCondition(_canEntryDistance);
            return clone;
        }

        [SerializeField] private float _canEntryDistance;
    }
    #endregion

    /// <summary> 現在のフェーズ数が指定フェーズと一致する場合にエントリーを許可する機能装飾ノード </summary>
    #region CurrentPhaseDecoratorNode
    [Serializable]
    public class CurrentPhaseDecoratorNode : DecoratorNode<CurrentPhaseDecoratorNode>
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

        public override CurrentPhaseDecoratorNode Clone()
        {
            var clone = new CurrentPhaseDecoratorNode();
            clone.SetCanEntryCondition(_canEntryPhase);
            return clone;
        }

        [SerializeField] private int _canEntryPhase;
    }
    #endregion

    /// <summary> 現在のボスのHPが指定した比較条件を満たす場合にエントリーを許可する機能装飾ノード </summary>
    #region CharacterHPDecoratorNode
    [Serializable]
    public class CharacterHPDecoratorNode : DecoratorNode<CharacterHPDecoratorNode>
    {
        public void SetCanEntryCondition(InequalityType numericalComparisonType, int canEntryRemainingHP)
        {
            _numericalComparisonType = numericalComparisonType;
            _canEntryRemainingHP = canEntryRemainingHP;
        }

        public override NodeCondition TryEntry()
        {
            if (_bossCharacterEntity == null)
            {
                Debug.LogError("CharacterHPDecoratorNodeが初期化されていません。BehaviourTreeをEntityとともにInitしてください。");
                return NodeCondition.Failure;
            }

            var currentHP = _bossCharacterEntity.CurrentHP;
            if (currentHP == null)
            {
                Debug.LogError("CharacterHPDecoratorNodeのCurrentHPが未初期化です。Entity.Init()完了後にBehaviourTreeを開始してください。");
                return NodeCondition.Failure;
            }

            switch (_numericalComparisonType)
            {
                case InequalityType.Greater:
                    if(currentHP.Value < _canEntryRemainingHP)
                        return NodeCondition.Success;
                    break;
                case InequalityType.Less:
                    if(currentHP.Value > _canEntryRemainingHP)
                        return NodeCondition.Success;
                    break;
                case InequalityType.Equals:
                    if (currentHP.Value == _canEntryRemainingHP)
                        return NodeCondition.Success;
                    break;
            }

            return NodeCondition.Failure;
        }

        public override CharacterHPDecoratorNode Clone()
        {
            var clone = new CharacterHPDecoratorNode();
            clone.SetCanEntryCondition(_numericalComparisonType, _canEntryRemainingHP);
            return clone;
        }

        [SerializeField] private InequalityType _numericalComparisonType;

        [SerializeField] private int _canEntryRemainingHP;
    }
    #endregion

    /// <summary> 指定回数の試行ごとにエントリーを許可するカウントダウン式機能装飾ノード </summary>
    #region CountDownDecoratorNode
    [Serializable]
    public class CountDownDecoratorNode : DecoratorNode<CountDownDecoratorNode>
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

        public override CountDownDecoratorNode Clone()
        {
            var clone = new CountDownDecoratorNode();
            clone.SetCanEntryCondition(_canEntryCount);
            return clone;
        }

        [SerializeField] private int _canEntryCount;

        private int _currentCount = 0;
    }
    #endregion
}
