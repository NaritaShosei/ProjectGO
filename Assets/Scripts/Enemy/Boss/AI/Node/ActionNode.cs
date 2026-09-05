using BossEnemy.Attack;
using BossEnemy.Enum;
using BossEnemy.Interface;
using BossEnemy.Logic;
using System;
using UnityEngine;
using UniRx;


namespace BossEnemy.AI.BehaviourTree
{
    /// <summary> 行動の実行を行うNode(最終的なTree構造の最深部) </summary>
    [Serializable]
    public abstract class ActionNode : BossCharacterBehaviourTreeNode
    {
        public override NodeCondition TryEntry()
        {
            return NodeCondition.Success;
        }

        public override NodeCondition TryEntryNextNode(out ITreeNode nextNode)
        {
            nextNode = this;
            return NodeCondition.Running;
        }
    }

    [Serializable]
    public class AwaitAction : ActionNode
    {

    }

    [Serializable]
    public class PostureChangeAction : ActionNode
    {
        public void SetChangePosture(PostureType postureType)
        {
            _changePosture = postureType;
        }

        public override void OnEnter()
        {
            _bossCharacterEntity.SetCharacterPosture(_changePosture);
        }

        [SerializeField] private PostureType _changePosture;
    }

    [Serializable]
    public class SelectAttackAction : ActionNode
    {
        public override void OnEnter()
        {
            _bossCharacterEntity.SelectNextAttackData(_attackSelectPoolID);

            HandleRunningEnd();
        }

        public void SetAttackSelectPoolID(int id)
        {
            _attackSelectPoolID = id;
        }

        [SerializeField] private int _attackSelectPoolID;
    }

    [Serializable]
    public class TargetChaseAction : ActionNode
    {
        public void SetChaseSpeed(float moveSpeed)
        {
            _moveSpeed = moveSpeed;
        }

        public override void OnEnter()
        {
            var toTargetDistance =
                Vector3.Distance(_bossCharacterEntity.Position.Value,
                _bossCharacterEntity.AttackTarget.GetTargetCenter().position);

            if (_bossCharacterEntity.ExecutingAttackData.AttackStartDistance > toTargetDistance)
            {
                _nodeRunningConditionNotifier.HandleRunningEnd();
            }
        }

        public override void OnUpdate()
        {
            Movement.MoveTargetPosition(
                _bossCharacterEntity,
                _bossCharacterEntity.AttackTarget.GetTargetCenter().position,
                _moveSpeed,
                _bossCharacterEntity.TimeScale);

            var toTargetDistance =
                Vector3.Distance(_bossCharacterEntity.Position.Value,
                _bossCharacterEntity.AttackTarget.GetTargetCenter().position);

            if (_bossCharacterEntity.ExecutingAttackData.AttackStartDistance > toTargetDistance)
            {
                _nodeRunningConditionNotifier.HandleRunningEnd();
            }
        }

        [SerializeField] float _moveSpeed = 1.0f;
    }

    [Serializable]
    public class LookAtTargetAction : ActionNode
    {
        public void SetLookConditions(float lookSpeed, float finishAngleThreshold)
        {
            _lookSpeed = lookSpeed;
            _finishAngleThreshold = finishAngleThreshold;
        }

        public override void OnUpdate()
        {
            Movement.LookAtTarget(
                _bossCharacterEntity,
                _bossCharacterEntity.AttackTarget.GetTargetCenter().position,
                _lookSpeed,
                _finishAngleThreshold,
                out bool isLookAtTarget,
                _bossCharacterEntity.TimeScale);

            if (isLookAtTarget) _nodeRunningConditionNotifier.HandleRunningEnd();
        }

        [Header("振り向き速度")]
        [SerializeField] float _lookSpeed;

        [Header("Targetの方向を向いていると判定できる振り向き方向の最小誤差")]
        [SerializeField] float _finishAngleThreshold;
    }

    [Serializable]
    public class AttackExecuteAction : ActionNode
    {
        public override void OnEnter()
        {
            _bossCharacterEntity.ExecuteAttack();

            _subscription = _bossCharacterEntity.IsAttacking.Subscribe(isAttacking =>
            {
                if(!isAttacking) HandleRunningEnd();
            });
        }

        public override void OnExit()
        {
            // 個別に購読を解除
            _subscription?.Dispose();
            _subscription = null;
        }

        private IDisposable _subscription;
    }

    [Serializable]
    public class DeadAction : ActionNode
    {
        public override void OnEnter()
        {
            _bossCharacterEntity.HandleDead();
            _nodeRunningConditionNotifier.HandleRunningEnd();
        }
    }

    [Serializable]
    public class PhaseChangeAction : ActionNode
    {
        public override void OnEnter()
        {
            _bossCharacterEntity.OnPhaseChange();

            _subscription = _bossCharacterEntity.IsPhaseChaging.Subscribe(isPhaseChanging =>
            {
                if(!isPhaseChanging) HandleRunningEnd();
            });
        }

        public override void OnExit()
        {
            // 個別に購読を解除
            _subscription?.Dispose();
            _subscription = null;
        }

        private IDisposable _subscription;
    }
}
