using BossEnemy.AI.BehaviourTree;
using BossEnemy.Attack;
using BossEnemy.Enum;
using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace BossEnemy.AI.Editor.BehaviourGraph
{
    [Serializable]
    public class ActionNode<TNode> : BehaviourTreeNodeView<TNode> where TNode : ActionNode
    {
        public ActionNode()
        {
            _nodeAccesskey = CreateUniqueKey();
        }

        // ポートの定義
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            base.OnDefinePorts(context);

            context
                .AddInputPort<TreeNode>(_nodeAccesskey)
                .WithDisplayName(ENTRY_PORT_NAME)
                .Build();
        }
    }

    [Serializable]
    public class AwaitAction : ActionNode<BehaviourTree.AwaitAction>
    {
        public AwaitAction()
        {
            _nodeAccesskey = CreateUniqueKey();
            _behaviourTreeNode = new BehaviourTree.AwaitAction();
        }
    }

    [Serializable]
    public class PostureChangeAction : ActionNode<BehaviourTree.PostureChangeAction>
    {
        private string CHANGE_POSTURE_NAME = "次の姿勢";

        public PostureChangeAction()
        {
            _nodeAccesskey = CreateUniqueKey();
            _behaviourTreeNode = new BehaviourTree.PostureChangeAction();
        }

        public override void OnGraphChanged(GraphLogger graphLogger)
        {
            base.OnGraphChanged(graphLogger);

            if (GetNodeOptionByName(CHANGE_POSTURE_NAME).TryGetValue(out PostureType postureType))
            {
                _behaviourTreeNode.SetChangePosture(postureType);
            }
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            base.OnDefineOptions(context);

            context
                .AddOption<PostureType>(CHANGE_POSTURE_NAME)
                .WithDisplayName(CHANGE_POSTURE_NAME)
                .Build();
        }
    }

    [Serializable]
    public class SelectAttackAction : ActionNode<BehaviourTree.SelectAttackAction>
    {
        private const string SELECT_POOL_ID = "攻撃の選択肢ID";

        public SelectAttackAction()
        {
            _nodeAccesskey = CreateUniqueKey();
            _behaviourTreeNode = new BehaviourTree.SelectAttackAction();
        }

        public override void OnGraphChanged(GraphLogger graphLogger)
        {
            base.OnGraphChanged(graphLogger);

            if (GetNodeOptionByName(SELECT_POOL_ID).TryGetValue(out int attackPoolID))
            {
                _behaviourTreeNode.SetAttackSelectPoolID(attackPoolID);
            }
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            base.OnDefineOptions(context);

            context.AddOption<int>(SELECT_POOL_ID).Build();
        }
    }

    [Serializable]
    public class TargetChaseAction : ActionNode<BehaviourTree.TargetChaseAction>
    {
        private const string CHASE_SPEED = "追いかける速度";

        public TargetChaseAction()
        {
            _nodeAccesskey = CreateUniqueKey();
            _behaviourTreeNode = new BehaviourTree.TargetChaseAction();
        }

        public override void OnGraphChanged(GraphLogger graphLogger)
        {
            base.OnGraphChanged(graphLogger);

            if (GetNodeOptionByName(CHASE_SPEED).TryGetValue(out float moveSpeed))
            {
                _behaviourTreeNode.SetChaseSpeed(moveSpeed);
            }
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            base.OnDefineOptions(context);

            context.AddOption<float>(CHASE_SPEED)
                .WithDisplayName(CHASE_SPEED)
                .Build();
        }
    }

    [Serializable]
    public class AttackExecuteAction : ActionNode<BehaviourTree.AttackExecuteAction>
    {
        public AttackExecuteAction()
        {
            _nodeAccesskey = CreateUniqueKey();
            _behaviourTreeNode = new BehaviourTree.AttackExecuteAction();
        }
    }

    [Serializable]
    public class LookAtTargetAction : ActionNode<BehaviourTree.LookAtTargetAction>
    {
        private const string LOOK_SPEED = "振り向き速度";

        private const string FINISH_ANGLE_THREHOLD = "許される振り向き誤差";

        public LookAtTargetAction()
        {
            _nodeAccesskey = CreateUniqueKey();
            _behaviourTreeNode = new BehaviourTree.LookAtTargetAction();
        }

        public override void OnGraphChanged(GraphLogger graphLogger)
        {
            base.OnGraphChanged(graphLogger);

            if (!GetNodeOptionByName(LOOK_SPEED).TryGetValue(out float lookSpeed)) return;

            if (!GetNodeOptionByName(FINISH_ANGLE_THREHOLD).TryGetValue(out float finishAngleThreshold)) return;

            _behaviourTreeNode.SetLookConditions(lookSpeed, finishAngleThreshold);
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            base.OnDefineOptions(context);

            context
                .AddOption<float>(LOOK_SPEED)
                .WithDisplayName(LOOK_SPEED)
                .Build();

            context
                .AddOption<float>(FINISH_ANGLE_THREHOLD)
                .WithDisplayName(FINISH_ANGLE_THREHOLD)
                .Build();
        }
    }

    [Serializable]
    public class DeadAction : ActionNode<BehaviourTree.DeadAction>
    {
        public DeadAction()
        {
            _nodeAccesskey = CreateUniqueKey();
            _behaviourTreeNode = new BehaviourTree.DeadAction();
        }
    }

    [Serializable]
    public class PhaseChangeAction : ActionNode<BehaviourTree.PhaseChangeAction>
    {
        public PhaseChangeAction()
        {
            _nodeAccesskey = CreateUniqueKey();
            _behaviourTreeNode = new BehaviourTree.PhaseChangeAction();
        }
    }
}
