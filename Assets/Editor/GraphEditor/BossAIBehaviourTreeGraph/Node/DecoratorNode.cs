using BossEnemy.AI.BehaviourTree;
using BossEnemy.Enum;
using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using static Unity.GraphToolkit.Editor.Node;

namespace BossEnemy.AI.Editor.BehaviourGraph
{
    [Serializable]
    public class DecoratorNode<TNode> : BehaviourTreeNodeView<TNode> where TNode : BehaviourTree.DecoratorNode
    {
        public override void OnGraphChanged(GraphLogger graphLogger)
        {
            base.OnGraphChanged(graphLogger);

            // OutputPortが一つもなければ即座に次のループに移る
            if (outputPortCount == 0) return;

            // Nodeから子ノードへの接続を行うOutputPortを取得
            IPort childNodeEntryPort = GetOutputPortByName(CHILD_PORT_NAME);

            List<IPort> outConnectedPort = new List<IPort>();
            childNodeEntryPort.GetConnectedPorts(outConnectedPort);

            foreach (var edge in outConnectedPort)
            {
                if (_nodeAccesskey == edge.name) continue;
                _connectedNodesKey.Add(edge.name);
            }
        }

        // ポートの定義
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            base.OnDefinePorts(context);

            context
                .AddInputPort<TreeNode>(_nodeAccesskey)
                .WithDisplayName(ENTRY_PORT_NAME)
                .Build();

            context
                .AddOutputPort<TreeNode>(CHILD_PORT_NAME)
                .WithDisplayName(CHILD_PORT_NAME)
                .Build();
        }
    }

    [Serializable]
    public class ArmorBrokenDecoratorNode : DecoratorNode<BehaviourTree.ArmorBrokenDecoratorNode>
    {
        private const string BROKEN_ARMOR_COUNT = "Entry条件となる鎧破壊個所の総数";

        private const string BROKEN_ARMOR_ATTACHMENT_NAME = "破壊された鎧の装備ヶ所";

        public ArmorBrokenDecoratorNode()
        {
            _nodeAccesskey = CreateUniqueKey();
            _behaviourTreeNode = new BehaviourTree.ArmorBrokenDecoratorNode();
        }

        public override void OnEnable()
        {
            base.OnEnable();

            if (GetNodeOptionByName(BROKEN_ARMOR_COUNT).TryGetValue(out int brokenArmorCount))
            {
                _brokenArmorCount = brokenArmorCount;
            }
        }

        public override void OnGraphChanged(GraphLogger graphLogger)
        {
            base.OnGraphChanged(graphLogger);

            if (GetNodeOptionByName(BROKEN_ARMOR_COUNT).TryGetValue(out int brokenArmorCount))
            {
                _brokenArmorCount = brokenArmorCount;
            }

            ArmorAttachmentType[] brokenConditionArmorArray = new ArmorAttachmentType[_brokenArmorCount];

            for (int i = 0; i < _brokenArmorCount; i++)
            {
                if (GetNodeOptionByName(BROKEN_ARMOR_ATTACHMENT_NAME + "_" + i).TryGetValue(out ArmorAttachmentType brokenConditionArmor))
                {
                    brokenConditionArmorArray[i] = brokenConditionArmor;
                }
            }

            _behaviourTreeNode.SetCanEntryCondition(brokenConditionArmorArray);
        }

        private int _brokenArmorCount = 1;

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            base.OnDefineOptions(context);

            context.AddOption<int>(BROKEN_ARMOR_COUNT).Build();

            for (int i = 0; i < _brokenArmorCount; i++)
            {
                context
                    .AddOption<ArmorAttachmentType>(BROKEN_ARMOR_ATTACHMENT_NAME + "_" + i)
                    .Build();
            }
        }
    }

    [Serializable]
    public class BossCharacterPostureDecoratorNode : DecoratorNode<BehaviourTree.BossCharacterPostureDecoratorNode>
    {
        private const string CAN_ENTRY_POSTURE_NAME = "Entry条件となるBossの姿勢";

        public BossCharacterPostureDecoratorNode()
        {
            _nodeAccesskey = CreateUniqueKey();
            _behaviourTreeNode = new BehaviourTree.BossCharacterPostureDecoratorNode();
        }

        public override void OnGraphChanged(GraphLogger graphLogger)
        {
            base.OnGraphChanged(graphLogger);

            if (GetNodeOptionByName(CAN_ENTRY_POSTURE_NAME).TryGetValue(out PostureType postureType))
            {
                _behaviourTreeNode.SetCanEntryCondition(postureType);
            }
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            base.OnDefineOptions(context);

            context.AddOption<PostureType>(CAN_ENTRY_POSTURE_NAME).Build();
        }
    }

    [Serializable]
    public class TargetDistanceDecoratorNode : DecoratorNode<BehaviourTree.TargetDistanceDecoratorNode>
    {
        private const string CAN_ENTRY_DISTANCE = "Entry可能な接近距離";

        public TargetDistanceDecoratorNode()
        {
            _nodeAccesskey = CreateUniqueKey();
            _behaviourTreeNode = new BehaviourTree.TargetDistanceDecoratorNode();
        }

        public override void OnGraphChanged(GraphLogger graphLogger)
        {
            base.OnGraphChanged(graphLogger);

            if (GetNodeOptionByName(CAN_ENTRY_DISTANCE).TryGetValue(out float distance))
            {
                _behaviourTreeNode.SetCanEntryCondition(distance);
            }
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            base.OnDefineOptions(context);

            context
                .AddOption<float>(CAN_ENTRY_DISTANCE)
                .WithDisplayName(CAN_ENTRY_DISTANCE)
                .Build();
        }
    }

    [Serializable]
    public class CurrentPhaseDecoratorNode : DecoratorNode<BehaviourTree.CurrentPhaseDecoratorNode>
    {
        private const string CAN_ENTRY_PHASE_NUMBER = "エントリー可能なPhase数";

        public CurrentPhaseDecoratorNode()
        {
            _nodeAccesskey = CreateUniqueKey();
            _behaviourTreeNode = new BehaviourTree.CurrentPhaseDecoratorNode();
        }

        public override void OnGraphChanged(GraphLogger graphLogger)
        {
            base.OnGraphChanged(graphLogger);

            if (GetNodeOptionByName(CAN_ENTRY_PHASE_NUMBER).TryGetValue(out int num))
            {
                _behaviourTreeNode.SetCanEntryCondition(num);
            }
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            base.OnDefineOptions(context);

            context
                .AddOption<int>(CAN_ENTRY_PHASE_NUMBER)
                .WithDisplayName(CAN_ENTRY_PHASE_NUMBER)
                .Build();
        }
    }

    [Serializable]
    public class CharacterHPDecoratorNode : DecoratorNode<BehaviourTree.CharacterHPDecoratorNode>
    {
        private const string CAN_ENTRY_INEQUALITY = "エントリーの際のHPの比較不等式の種類";

        private const string CAN_ENTRY_REMAINING_HP = "エントリー可能になる残りHP";

        public CharacterHPDecoratorNode()
        {
            _nodeAccesskey = CreateUniqueKey();
            _behaviourTreeNode = new BehaviourTree.CharacterHPDecoratorNode();
        }

        public override void OnGraphChanged(GraphLogger graphLogger)
        {
            base.OnGraphChanged(graphLogger);

            if (!GetNodeOptionByName(CAN_ENTRY_INEQUALITY).TryGetValue(out InequalityType canEntryInequalityType)) return;

            if (!GetNodeOptionByName(CAN_ENTRY_REMAINING_HP).TryGetValue(out int remainingHP)) return;

            _behaviourTreeNode.SetCanEntryCondition(canEntryInequalityType, remainingHP);
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            base.OnDefineOptions(context);

            context
                .AddOption<InequalityType>(CAN_ENTRY_INEQUALITY)
                .WithDisplayName(CAN_ENTRY_INEQUALITY)
                .Build();

            context
                .AddOption<int>(CAN_ENTRY_REMAINING_HP)
                .WithDisplayName(CAN_ENTRY_REMAINING_HP)
                .Build();
        }
    }

    [Serializable]
    public class CountDownDecoratorNode : DecoratorNode<BehaviourTree.CountDownDecoratorNode>
    {
        private const string CAN_ENTRY_COUNT = "エントリー可能になるカウント数";

        public CountDownDecoratorNode()
        {
            _nodeAccesskey = CreateUniqueKey();
            _behaviourTreeNode = new BehaviourTree.CountDownDecoratorNode();
        }

        public override void OnGraphChanged(GraphLogger graphLogger)
        {
            base.OnGraphChanged(graphLogger);

            if (!GetNodeOptionByName(CAN_ENTRY_COUNT).TryGetValue(out int canEntryCount)) return;

            _behaviourTreeNode.SetCanEntryCondition(canEntryCount);
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            base.OnDefineOptions(context);

            context
                .AddOption<int>(CAN_ENTRY_COUNT)
                .WithDisplayName(CAN_ENTRY_COUNT)
                .Build();
        }
    }
}
