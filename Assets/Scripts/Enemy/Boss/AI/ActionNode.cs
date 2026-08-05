using UnityEngine;

using BossEnemy.Character;
using BossEnemy.System;

namespace BossEnemy.BehaviorTree
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
            Debug.Log("攻撃終了");

            // 攻撃開始直後に終了通知が来ても取りこぼさないよう、先に購読してから攻撃を開始する。
            _attack.OnAttackFinish += RunningEnd;
            _attack.AttackActionStart(_bossEnemyAttackData);
        }

        public override void OnExit()
        {
            Debug.Log("攻撃終了");
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
            Status bossEnemyData,
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

        private readonly Status _bossData;

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
            Status bossEnemyData,
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
        public DownAction(Status bossEnemyData, BossDown bossDown, float oneLegBreakDownTime, float allLegBreakDownTime)
        {
            _data = bossEnemyData;
            _bossDown = bossDown;
            _oneLegBreakDownTime = oneLegBreakDownTime;
            _allLegBreakDownTime = allLegBreakDownTime;
        }

        public bool IsDown => _isDown;

        public override void OnEnter()
        {
            Debug.Log("EntryDownAction");
            _isDown = true;
            _isRiseUp = false;

            _bossDown.Down();

            if(_data.LeftLegArmer.IsArmorBreak && _data.RightLegArmer.IsArmorBreak)
            {
                _downTime = _allLegBreakDownTime;
                return;
            }

            _downTime = _oneLegBreakDownTime;
        }

        public override void OnUpdate()
        {
            // 前回のフレームからの経過時間を足していく
            _elapsedTime += Time.deltaTime;

            if (_elapsedTime >= _downTime && !_isRiseUp)
            {
                Debug.Log("立ち上がりーよ");
                _bossDown.RiseUp();
                RunningEnd();
                _isRiseUp = true;
            }
        }

        public override void OnExit()
        {
            if(_elapsedTime < _downTime && !_isRiseUp)
            {
                Debug.Log("立ち上がりーよ");
                _bossDown.RiseUp();
            }

            _isDown = false;
            _elapsedTime = 0;
            _downTime = 0;
        }

        private float _downTime = 0;
        private float _elapsedTime = 0f;

        private Status _data;
        private BossDown _bossDown;
        
        private readonly float _oneLegBreakDownTime;
        private readonly float _allLegBreakDownTime;

        private bool _isDown = false;
        private bool _isRiseUp = false;
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
