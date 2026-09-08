using BossEnemy.Attack;
using BossEnemy.Enum;
using BossEnemy.Interface;
using BossEnemy.Logic;
using System;
using Cysharp.Threading.Tasks;
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
            SelectNextAttackAsync().Forget();
        }

        public void SetAttackSelectPoolID(int id)
        {
            _attackSelectPoolID = id;
        }

        [SerializeField] private int _attackSelectPoolID;

        private async UniTaskVoid SelectNextAttackAsync()
        {
            await _bossCharacterEntity.SelectNextAttackData(_attackSelectPoolID);
            HandleRunningEnd();
        }
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
            Attack.AttackData nextAttackData = _bossCharacterEntity.GetNextAttackData();
            if (nextAttackData.AttackStartDistance == 0)
            {
                HandleRunningEnd();
                return;
            }

            var toTargetDistance =
                Vector3.Distance(_bossCharacterEntity.Position.Value,
                _bossCharacterEntity.AttackTarget.GetTargetCenter().position);

            // 攻撃開始位置よりも敵が近ければ攻撃を行う
            if (nextAttackData.AttackStartDistance > toTargetDistance)
            {
                HandleRunningEnd();
            }
        }

        public override void OnUpdate()
        {
            Attack.AttackData nextAttackData = _bossCharacterEntity.GetNextAttackData();

            Movement.MoveTargetPosition(
                _bossCharacterEntity,
                _bossCharacterEntity.AttackTarget.GetTargetCenter().position,
                _moveSpeed,
                _bossCharacterEntity.TimeScale);

            var toTargetDistance =
                Vector3.Distance(_bossCharacterEntity.Position.Value,
                _bossCharacterEntity.AttackTarget.GetTargetCenter().position);

            if (nextAttackData.AttackStartDistance > toTargetDistance)
            {
                HandleRunningEnd();
            }
        }

        public override void OnExit()
        {
            _bossCharacterEntity.SetVelocity(Vector3.zero);
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

            if (isLookAtTarget) _nodeRunningConditionNotifier.HandleResearchBehaviourTree();
        }

        public override void OnExit()
        {
            _bossCharacterEntity.SetVelocity(Vector3.zero);
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

            _disposable = _bossCharacterEntity.ExecutingAttackData.Subscribe(executingAttackData =>
            {
                if(executingAttackData.ID == 0)
                {
                    HandleRunningEnd();
                }
            });
        }

        public override void OnExit()
        {
            _disposable?.Dispose();
            _disposable = null;
        }

        IDisposable _disposable = null;
    }

    [Serializable]
    public class DeadAction : ActionNode
    {
        public override void OnEnter()
        {
            _bossCharacterEntity.HandleDead();
        }
    }

    [Serializable]
    public class PhaseChangeAction : ActionNode
    {
        public override void OnEnter()
        {
            _bossCharacterEntity.PhaseChange();
        }
    }
}
