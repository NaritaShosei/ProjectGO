using System;
using UniRx;
using UnityEngine;

# region BossEnemy関連のusing
using BossEnemy.Data;
using BossEnemy.Data.Repositry;
using BossEnemy.Model.CoreLogic;
# endregion

namespace BossEnemy.BehaviorTree.Node.ActionNode
{
    /// <summary> 行動の実行を行うNode(最終的なTree構造の最深部) </summary>
    public abstract class ActionNode : TreeNodeBase
    {
        public override NodeCondition TryEntry()
        {
            return NodeCondition.Success;
        }

        public override NodeCondition TryGetNextNode(ref ITreeNode nextNode)
        {
            return NodeCondition.Running;
        }
    }

    #region 攻撃アクション
    public class AttackAction : ActionNode
    {
        public AttackAction(BossAttack bossAttack)
        {
            _attack = bossAttack;
        }

        public void SetNextAttackData(BossEnemyAttackData bossEnemyAttackData)
        {
            _bossEnemyAttackData = bossEnemyAttackData;
        }

        public override void OnEnter()
        {
            _attack.AttackActionStart(_bossEnemyAttackData);
            _attack.OnAttackFinish += RunningEnd;
        }

        public override void OnExit()
        {
            _attack.OnAttackFinish -= RunningEnd;
        }

        private BossEnemyAttackData _bossEnemyAttackData;

        private readonly BossAttack _attack;
    }
    #endregion

    #region ターゲット追尾アクション
    public class TargetChaseAction : ActionNode
    {
        public TargetChaseAction(Transform target, BossMove bossEnemyMove, BossEnemyData bossEnemyData, IPlayerInformationService playerInformationService)
        {
            _target = target;
            _bossEnemyMove = bossEnemyMove;
            _bossData = bossEnemyData;
            _playerInformationService = playerInformationService;
        }

        public void SetGoalDistance(float distance)
        {
            _goalDistance = distance;
        }

        public override void OnEnter()
        {
            Debug.Log("追跡開始");

            if (_goalDistance == 0) RunningEnd();
        }

        public override void OnUpdate()
        {
            _bossEnemyMove.MoveTargetPosition(_target, _bossData.WalkSpeed);

            if (_goalDistance >= _playerInformationService.ToPlayerDistance(_bossData.Position.Value))
            {
                _bossEnemyMove.StopMove();
                RunningEnd();
            }
        }

        public override void OnExit()
        {
            Debug.Log("追跡終了");
        }

        private float _goalDistance = 0;

        private Transform _target;

        private readonly BossMove _bossEnemyMove;

        private BossEnemyData _bossData;

        private IPlayerInformationService _playerInformationService;
    }
    #endregion

    public class LookAtTargetAction : ActionNode
    {
        public LookAtTargetAction(Transform target, BossMove bossEnemyMove, BossEnemyData bossEnemyData, 
            IPlayerInformationService playerInformationService, float lookSpeed, float finishAngleThreshold)
        {
            _target = target;
            _bossEnemyMove = bossEnemyMove;
            _bossData = bossEnemyData;
            _playerInformationService = playerInformationService;
            _lookSpeed = lookSpeed;
            _finishAngleThreshold = finishAngleThreshold;
        }

        public override void OnUpdate()
        {
            _bossEnemyMove.LookAtTarget(_target, _lookSpeed, _finishAngleThreshold, out bool isFinish);

            if (isFinish)
            {
                RunningEnd();
            }
        }

        private float _lookSpeed;

        private float _finishAngleThreshold;

        private Transform _target;

        private readonly BossMove _bossEnemyMove;

        private BossEnemyData _bossData;

        private IPlayerInformationService _playerInformationService;
    }

    #region ダウンした時のアクション
    public class DownAction : ActionNode
    {

    }
    #endregion

    #region Phaseチェンジアクション
    /// <summary> ボスのPhase変更時のNode </summary>
    public class PhaseChangeAction : ActionNode
    {

    }
    #endregion

    #region 負けた時のアクション
    /// <summary> ボス討伐成功時のNode </summary>
    public class DefeatAction : ActionNode
    {

    }
    #endregion

    #region NullAction
    /// <summary> 仮置きNode </summary>
    public class NullAction : ActionNode
    {
        public override NodeCondition TryEntry()
        {
            return NodeCondition.Failure;
        }
    }
    #endregion
}
