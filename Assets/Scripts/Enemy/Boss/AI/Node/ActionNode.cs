using BossEnemy.Attack;
using BossEnemy.Character;
using BossEnemy.Enum;
using BossEnemy.Interface;
using BossEnemy.Logic;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UniRx;
using UnityEngine;


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
    public class WaitForTimeAction : ActionNode
    {
        public void SetWaitTime(float waitTime)
        {
            _waitTime = waitTime;
        }

        public override void OnEnter()
        {
            _isTimeUp = false;
            _currentWaitTime = _waitTime;
        }

        public override void OnUpdate()
        {
            if (!_isTimeUp)
            {
                _currentWaitTime -= Time.deltaTime * _bossCharacterEntity.TimeScale;

                if (_currentWaitTime < 0)
                {
                    _isTimeUp = true;
                    HandleRunningEnd();
                }
            }
        }

        [SerializeField] private float _waitTime;

        private bool _isTimeUp = false;

        private float _currentWaitTime = 0;
    }

    [Serializable]
    public class CancelSearchAction : ActionNode
    {
        public override NodeCondition TryEntry()
        {
            return NodeCondition.Failure;
        }

        public override NodeCondition TryEntryNextNode(out ITreeNode nextNode)
        {
            nextNode = null;
            return NodeCondition.Failure;
        }
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
            _isPostureChangeCompleted = false;
        }

        public override void OnUpdate()
        {
            if (_isPostureChangeCompleted) return;

            if (_bossCharacterEntity.CurrentAction.Value
                != CharacterAction.PostureChanging)
            {
                HandleRunningEnd();
                _isPostureChangeCompleted = true;
            }
        }

        [SerializeField] private PostureType _changePosture;

        private bool _isPostureChangeCompleted = true;
    }

    [Serializable]
    public class PostureRevertInSecondsAction : ActionNode 
    {
        public void SetConditions(PostureType postureType, float revertInSeconds)
        {
            _changePosture = postureType;
            _revertInSeconds = revertInSeconds;
        }

        public override void OnEnter()
        {
            CancelRevert();

            var cts = new CancellationTokenSource();
            _cancellationTokenSource = cts;

            RevertInSecondsAsync(_cancellationTokenSource.Token).Forget();

            HandleRunningEnd();
        }

        [SerializeField] private PostureType _changePosture;

        [SerializeField] private float _revertInSeconds;

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private void CancelRevert()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        private async UniTaskVoid RevertInSecondsAsync(CancellationToken token)
        {
            try
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(_revertInSeconds),
                    cancellationToken: token);

                _bossCharacterEntity.SetCharacterPosture(_changePosture);

                await UniTask.WaitUntil(
                    () => _bossCharacterEntity.CurrentAction.Value
                        != CharacterAction.PostureChanging,
                    cancellationToken: token);

                if (!token.IsCancellationRequested)
                    HandleRunningEnd();
            }
            catch (OperationCanceledException)
            {
            }
        }
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

        /// <summary> 攻撃の選択を行う </summary>
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
            _bossCharacterEntity.SetCurrentAction(Character.CharacterAction.Walking);

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
            _bossCharacterEntity.SetCurrentAction(Character.CharacterAction.Idle);
            _bossCharacterEntity.SetVelocity(Vector3.zero);
        }

        [SerializeField] private float _moveSpeed = 1.0f;
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
        [SerializeField] private float _lookSpeed;

        [Header("Targetの方向を向いていると判定できる振り向き方向の最小誤差")]
        [SerializeField] private float _finishAngleThreshold;
    }

    [Serializable]
    public class AttackExecuteAction : ActionNode
    {
        public override void OnEnter()
        {
            _disposable?.Dispose();
            _disposable = null;

            _bossCharacterEntity.ExecuteAttack();

            _disposable = _bossCharacterEntity.CurrentAction
                .SkipLatestValueOnSubscribe()
                .Subscribe(currentAction =>
            {
                if(currentAction != Character.CharacterAction.Attacking)
                    HandleRunningEnd();
            });
        }

        public override void OnExit()
        {
            _disposable?.Dispose();
            _disposable = null;
        }

        private IDisposable _disposable = null;
    }

    [Serializable]
    public class DeadAction : ActionNode
    {
        public override void OnEnter()
        {
            _bossCharacterEntity.SetCurrentAction(Character.CharacterAction.Dead);
        }
    }

    [Serializable]
    public class PhaseChangeAction : ActionNode
    {
        public override void OnEnter()
        {
            _bossCharacterEntity.StartPhaseChange();
            _isPhaseChangeCompleted = false;
        }

        public override void OnUpdate()
        {
            if (!_isPhaseChangeCompleted 
                && _bossCharacterEntity.CurrentAction.Value 
                != Character.CharacterAction.PhaseChanging)
            {
                _isPhaseChangeCompleted = true;
                HandleRunningEnd();
            }
        }

        private bool _isPhaseChangeCompleted = true;
    }
}
