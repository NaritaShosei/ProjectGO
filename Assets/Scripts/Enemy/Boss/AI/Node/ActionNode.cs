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
        /// <summary> 何らかの不具合で処理が詰まった際にツリーを強制再走させる秒数を設定 </summary>
        public void SetTimeOutValue(float timeOut)
        {
            _timeOutValue = timeOut;
        }

        public override NodeCondition TryEntry()
        {
            return NodeCondition.Success;
        }

        public override NodeCondition TryEntryNextNode(out ITreeNode nextNode)
        {
            nextNode = this;
            return NodeCondition.Running;
        }

        public override void OnEnter()
        {
            _timeOutTimer = 0f;
        }

        public override void OnUpdate()
        {
            if (IsTimeOut()) HandleRunningEnd();
        }

        [SerializeField] private float _timeOutValue = 10f;

        protected float _timeOutTimer = 0;

        protected bool IsTimeOut()
        {
            _timeOutTimer += Time.deltaTime * _bossCharacterEntity.TimeScale;

            if (_timeOutTimer >= _timeOutValue)
            {
                return true;
            }
            return false;
        }
    }

    [Serializable]
    public class ArmorRepairAction : ActionNode
    {
        public void SetRepairArmor(ArmorAttachmentType repairArmor)
        {
            _repairArmor = repairArmor;
        }

        public override void OnEnter()
        {
            base.OnEnter();

            if (_repairArmor == ArmorAttachmentType.None)
            {
                HandleRunningEnd();
                return;
            }

            _bossCharacterEntity.RepairArmor(_repairArmor);
        }

        public override void OnUpdate()
        {
            if(_bossCharacterEntity.RepairArmorAttachmentType.Value
                == ArmorAttachmentType.None)
            {
                HandleRunningEnd();
                return;
            }

            base.OnUpdate();
        }

        [SerializeField] private ArmorAttachmentType _repairArmor;
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
            base.OnEnter();
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
            base.OnEnter();
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
                return;
            }

            base.OnUpdate();
        }

        [SerializeField] private PostureType _changePosture;

        private bool _isPostureChangeCompleted = true;
    }

    [Serializable]
    public class SelectAttackAction : ActionNode
    {
        public override void Dispose()
        {
            // 完了待ちの選択結果が、破棄済みのシーケンスを進めないよう無効化する。
            _selectionVersion++;
            base.Dispose();
        }

        public override void OnEnter()
        {
            base.OnEnter();
            int selectionVersion = ++_selectionVersion;
            SelectNextAttackAsync(selectionVersion).Forget();
        }

        public override void OnExit()
        {
            // 攻撃選択中に別の行動へ割り込まれた場合、古い完了通知を無視する。
            _selectionVersion++;
        }

        public void SetAttackSelectPoolID(int id)
        {
            _attackSelectPoolID = id;
        }

        [SerializeField] private int _attackSelectPoolID;

        /// <summary> 攻撃の選択を行う </summary>
        private async UniTaskVoid SelectNextAttackAsync(int selectionVersion)
        {
            await _bossCharacterEntity.SelectNextAttackData(_attackSelectPoolID);

            if (selectionVersion != _selectionVersion
                || _nodeRunningConditionNotifier == null)
            {
                return;
            }

            HandleRunningEnd();
        }

        private int _selectionVersion;
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
            base.OnEnter();

            Attack.AttackData nextAttackData = _bossCharacterEntity.GetNextAttackData();

            if (nextAttackData.AttackStartDistance == 0)
            {
                HandleRunningEnd();
                return;
            }

            // ターゲットとの距離を取得
            var toTargetDistance = GetTargetDistance();

            // 攻撃開始位置よりも敵が近ければ攻撃を行う
            if (nextAttackData.AttackStartDistance > toTargetDistance)
            {
                HandleRunningEnd();
                return;
            }

            _bossCharacterEntity.SetCurrentAction(CharacterAction.Walking);
        }

        public override void OnUpdate()
        {
            Attack.AttackData nextAttackData = _bossCharacterEntity.GetNextAttackData();

            Logic.Movement.MoveTargetPosition(
                _bossCharacterEntity,
                _bossCharacterEntity.AttackTarget.GetTargetCenter().position,
                _moveSpeed,
                _bossCharacterEntity.TimeScale);

            // ターゲットとの距離を取得
            var toTargetDistance = GetTargetDistance();

            if (nextAttackData.AttackStartDistance > toTargetDistance)
            {
                HandleRunningEnd();
                return;
            }

            base.OnUpdate();
        }

        public override void OnExit()
        {
            _bossCharacterEntity.SetCurrentAction(Character.CharacterAction.Idle);
            _bossCharacterEntity.SetVelocity(Vector3.zero);
        }

        [SerializeField] private float _moveSpeed = 1.0f;

        /// <summary>
        /// ターゲットとの距離を取得
        /// </summary>
        private float GetTargetDistance()
        {
            // ボスの現在地(Y座標は無視する)
            var bossChracterPos = new Vector3()
            {
                x = _bossCharacterEntity.Position.Value.x,
                y = 0f,
                z = _bossCharacterEntity.Position.Value.z
            };

            // 攻撃対象の現在地(Y座標は無視する)
            var attackTargetPos = new Vector3()
            {
                x = _bossCharacterEntity.AttackTarget.GetTargetCenter().position.x,
                y = 0f,
                z = _bossCharacterEntity.AttackTarget.GetTargetCenter().position.z
            };

            // ターゲットとの距離を取得
            var toTargetDistance = Vector3.Distance(bossChracterPos, attackTargetPos);

            return toTargetDistance;
        }
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
            Logic.Movement.LookAtTarget(
                _bossCharacterEntity,
                _bossCharacterEntity.AttackTarget.GetTargetCenter().position,
                _lookSpeed,
                _finishAngleThreshold,
                out bool isLookAtTarget,
                _bossCharacterEntity.TimeScale);

            if (isLookAtTarget)
            {
                HandleRunningEnd();
                return;
            }

            base.OnUpdate();
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
            // ActionNode が持つタイムアウトを攻撃ごとにリセットする。
            // これを呼ばないと前回までの経過時間を引き継ぎ、後半の攻撃が即座に中断される。
            base.OnEnter();
            _hasFinished = false;
            _bossCharacterEntity.ExecuteAttack();
        }

        public override void OnUpdate()
        {
            if (_hasFinished) return;

            if (_bossCharacterEntity.CurrentAction.Value != CharacterAction.Attacking
                && _bossCharacterEntity.CurrentAction.Value != CharacterAction.PostureChanging)
            {
                _hasFinished = true;
                HandleRunningEnd();
                return;
            }

            if (IsTimeOut())
            {
                _bossCharacterEntity.CancelAttack();
                _bossCharacterEntity.SetCurrentAction(CharacterAction.Idle);

                _hasFinished = true;
                HandleRunningEnd();
            }
        }

        private bool _hasFinished;
    }

    [Serializable]
    public class DeadAction : ActionNode
    {
        public override void OnEnter()
        {
            _bossCharacterEntity.SetCurrentAction(CharacterAction.Dead);
        }

        public override void OnUpdate()
        {

        }
    }

    [Serializable]
    public class PhaseChangeAction : ActionNode
    {
        public override void OnEnter()
        {
            base.OnEnter();
            _bossCharacterEntity.StartPhaseChange();
            _isPhaseChangeCompleted = false;
        }

        public override void OnUpdate()
        {
            if (!_isPhaseChangeCompleted 
                && _bossCharacterEntity.CurrentAction.Value 
                != CharacterAction.PhaseChanging)
            {
                _isPhaseChangeCompleted = true;
                HandleRunningEnd();
                return;
            }

            base.OnUpdate();
        }

        private bool _isPhaseChangeCompleted = true;
    }

    [Serializable]
    public class CancelAsyncAction : ActionNode
    {
        public override void OnEnter()
        {
            _nodeRunningConditionNotifier.HandleCancelAsyncAction();

            HandleRunningEnd();
        }

        public override void OnUpdate() { }
    }

    [Serializable]
    public class RevertPostureInSecondsAsyncAction : ActionNode
    {
        public override void Dispose()
        {
            if (_nodeRunningConditionNotifier == null) return;

            Cancel();

            _nodeRunningConditionNotifier.OnCancelAsyncAction -= Cancel;

            base.Dispose();
        }

        public void SetConditions(PostureType postureType, float revertInSeconds)
        {
            _changePosture = postureType;
            _revertInSeconds = revertInSeconds;
        }

        public override void OnEnter()
        {
            Cancel();

            var cts = new CancellationTokenSource();
            _cancellationTokenSource = cts;

            _nodeRunningConditionNotifier.OnCancelAsyncAction += Cancel;

            RevertPostureInSecondsAsync(_cancellationTokenSource.Token).Forget();

            HandleRunningEnd();
        }

        public override void OnUpdate() { }

        [SerializeField] private PostureType _changePosture;

        [SerializeField] private float _revertInSeconds;

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        /// <summary> 時間経過による処理を中断処理 </summary>
        private void Cancel()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        /// <summary> 時間経過で体勢を変える処理 </summary>
        private async UniTaskVoid RevertPostureInSecondsAsync(CancellationToken token)
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
            finally
            {
                if(_nodeRunningConditionNotifier != null)
                    _nodeRunningConditionNotifier.OnCancelAsyncAction -= Cancel;
            }
        }
    }
}
