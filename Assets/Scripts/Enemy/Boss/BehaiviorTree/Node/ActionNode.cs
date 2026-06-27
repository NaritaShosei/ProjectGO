using UnityEngine;

#region BossEnemy関連のusing
using BossEnemy.Data;
using BossEnemy.Model.CoreLogic;
#endregion

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

        /// <summary> 次に実行する攻撃データを設定する </summary>
        public void SetNextAttackData(BossEnemyAttackData bossEnemyAttackData)
        {
            _bossEnemyAttackData = bossEnemyAttackData;
        }

        public override void OnEnter()
        {
            // 攻撃開始直後に終了通知が来ても取りこぼさないよう、先に購読してから攻撃を開始する。
            _attack.OnAttackFinish += RunningEnd;
            _attack.AttackActionStart(_bossEnemyAttackData);
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
        public TargetChaseAction(
            Transform target,
            BossMove bossEnemyMove,
            BossEnemyData bossEnemyData,
            IPlayerInformationService playerInformationService)
        {
            _target = target;
            _bossEnemyMove = bossEnemyMove;
            _bossData = bossEnemyData;
            _playerInformationService = playerInformationService;
        }

        /// <summary> 追跡を終了するプレイヤーとの距離を設定する </summary>
        public void SetGoalDistance(float distance)
        {
            _goalDistance = distance;
        }

        public override void OnEnter()
        {
            // 追跡不要な状態なら、その場で停止して次のNodeへ進める。
            if (_target == null || _goalDistance <= 0f || IsInGoalDistance())
            {
                StopAndFinish();
            }
        }

        public override void OnUpdate()
        {
            if (_target == null || IsInGoalDistance())
            {
                StopAndFinish();
                return;
            }

            _bossEnemyMove.MoveTargetPosition(_target, _bossData.WalkSpeed);

            if (IsInGoalDistance())
            {
                StopAndFinish();
            }
        }

        public override void OnExit()
        {
            _bossEnemyMove.StopMove();
        }

        private float _goalDistance = 0;

        private readonly Transform _target;

        private readonly BossMove _bossEnemyMove;

        private readonly BossEnemyData _bossData;

        private readonly IPlayerInformationService _playerInformationService;

        /// <summary> 現在の位置が攻撃可能距離に入っているか </summary>
        private bool IsInGoalDistance()
        {
            return _goalDistance >= _playerInformationService.ToPlayerDistance(_bossData.Position.Value);
        }

        private void StopAndFinish()
        {
            _bossEnemyMove.StopMove();
            RunningEnd();
        }
    }
    #endregion

    #region ターゲット方向への振り向きアクション
    public class LookAtTargetAction : ActionNode
    {
        public LookAtTargetAction(
            Transform target,
            BossMove bossEnemyMove,
            BossEnemyData bossEnemyData,
            IPlayerInformationService playerInformationService,
            float lookSpeed,
            float finishAngleThreshold)
        {
            _target = target;
            _bossEnemyMove = bossEnemyMove;
            _lookSpeed = lookSpeed;
            _finishAngleThreshold = finishAngleThreshold;
        }

        public override void OnEnter()
        {
            _bossEnemyMove.StopMove();
        }

        public override void OnUpdate()
        {
            if (_target == null)
            {
                StopAndFinish();
                return;
            }

            _bossEnemyMove.LookAtTarget(_target, _lookSpeed, _finishAngleThreshold, out bool isFinish);

            if (isFinish)
            {
                StopAndFinish();
            }
        }

        public override void OnExit()
        {
            _bossEnemyMove.StopMove();
        }

        private readonly float _lookSpeed;

        private readonly float _finishAngleThreshold;

        private readonly Transform _target;

        private readonly BossMove _bossEnemyMove;

        private void StopAndFinish()
        {
            _bossEnemyMove.StopMove();
            RunningEnd();
        }
    }
    #endregion

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
