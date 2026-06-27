using BossEnemy.BehaviorTree.Node.ActionNode;
using BossEnemy.Data;
using BossEnemy.Data.Repositry;
using BossEnemy.Model.CoreLogic;
using System;
using UniRx;
using UnityEngine;

namespace BossEnemy.BehaviorTree.Node.DecoratorNode
{
    /// <summary> 通ったら子Nodeを実行して、通らなければFailureを返すNode </summary>
    public abstract class DecoratorNode : TreeNodeBase
    {
        public DecoratorNode(ITreeNode childNode)
        {
            _childNode = childNode;
        }

        public override NodeCondition TryGetNextNode(ref ITreeNode nextNode)
        {
            if(_childNode == null)
            {
                Debug.LogError("子ノードがNullです");
                return NodeCondition.Failure;
            }

            NodeCondition condition = _childNode.TryEntry();

            if (condition == NodeCondition.Running || condition == NodeCondition.Success)
            {
                nextNode = _childNode;
                return NodeCondition.Success;
            }

            return NodeCondition.Failure;
        }

        /// <summary> 子ノード </summary>
        protected ITreeNode _childNode = null;
    }

    /// <summary> 攻撃の選択Node </summary>
    public class AttackSelect : DecoratorNode
    {
        public AttackSelect(ITreeNode child, 
            BossEnemyAttackField bossEnemyAttackField, 
            BossEnemyAttackDataRepositry bossEnemyAttackDataRepositry, 
            AttackAction attackNode, 
            TargetChaseAction targetChaseAction): base (child)
        {
            _bossEnemyAttackField = bossEnemyAttackField;
            _bossEnemyAttackDataRepositry = bossEnemyAttackDataRepositry;
            _attackNode = attackNode;
            _playerChase = targetChaseAction;
        }

        public override NodeCondition TryEntry()
        {
            int id = AttackDataSelector.GetRandamSelectAttackDataID(_bossEnemyAttackField);

            BossEnemyAttackData attackData = _bossEnemyAttackDataRepositry.GetData(id);

            Debug.Log("攻撃選択:" + attackData.Name);

            _attackNode.SetNextAttackData(attackData);
            _playerChase.SetGoalDistance(attackData.AttackHitDistance);

            return NodeCondition.Success;
        }

        private readonly BossEnemyAttackDataRepositry _bossEnemyAttackDataRepositry;

        private readonly BossEnemyAttackField _bossEnemyAttackField;

        private readonly AttackAction _attackNode;

        private readonly TargetChaseAction _playerChase;
    }

    #region 残りのHPに応じてEntryが可能となるDecoratorNode
    /// <summary> 残りのHPに応じてEntryが可能となるDecoratorNode </summary>
    public class BasedOnRemainingBossHPNode : DecoratorNode, IDisposable
    {
        /// <summary>
        /// 残りのHPに応じてEntryが可能となるDecoratorNode
        /// </summary>
        /// <param name="bossEnemyData"> 現在のBossEnemyのData </param>
        /// <param name="child"> 子ノード </param>
        /// <param name="entryHPLine"> Entryが可能となる残りのHP </param>
        /// <param name="isEntryAboveHP"> このフラグがTrueだとEntry条件が残りのHPをentryHPLineが上回っている時になる </param>
        public BasedOnRemainingBossHPNode(BossEnemyData bossEnemyData, ITreeNode child, int entryHPLine, bool isEntryAboveHP) : base (child) 
        {
            // 現在のBossのHPを取得し続ける
            bossEnemyData.CurrentHP.Subscribe(hp => _currentHP = hp ).AddTo(_disposables);

            // Entryが可能となるHPのLineを設定する
            _nodeEntryLine = entryHPLine;
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }

        public override NodeCondition TryEntry()
        {
            if( _childNode == null ) return NodeCondition.Failure;

            if (_isEntryHPAbove)
            {
                if (_currentHP > _nodeEntryLine) return NodeCondition.Success;
            }
            else
            {
                if (_currentHP <= _nodeEntryLine) return NodeCondition.Success;
            }

            return NodeCondition.Failure;
        }

        // 残りのHPがこの数値を下回ったときEntryが可能となる
        private readonly int _nodeEntryLine;

        // このフラグがTrueだとEntry条件が残りのHPを_entryHPLineが上回っている時になる
        private readonly bool _isEntryHPAbove; 

        // 現在のHP
        private int _currentHP;

        private readonly CompositeDisposable _disposables = new CompositeDisposable();
    }
    #endregion

    #region アーマーが破壊されていた場合にEntryが可能となるDecoratorNode
    /// <summary> アーマーが破壊されていた場合にEntryが可能となるDecoratorNode </summary>
    public class BreakArmorNode : DecoratorNode
    {
        public BreakArmorNode(ITreeNode child) : base(child)
        {
            _childNode = child;
        }

        public void CanEntry() => _canEntry = true;

        public override NodeCondition TryEntry()
        {
            if (_canEntry)
            {
                _canEntry = false;
                return NodeCondition.Success;
            }

            return NodeCondition.Failure;
        }

        private bool _canEntry = false;
    }
    #endregion

    #region Playerとの距離に応じてEntryが可能となるDecoratorNode
    /// <summary> Playerとの距離が一定数より近ければEntryが可能となるDecoratorNode </summary>
    public class CloseToPlayerNode : DecoratorNode
    {
        public CloseToPlayerNode(ITreeNode child, IPlayerInformationService playerInformationService,
            BossEnemyData bossEnemyData, float nodeEntryLine) : base(child)
        {
            _playerInformation = playerInformationService;
            _bossEnemyData = bossEnemyData;
            _nodeEntryLine = nodeEntryLine;
        }

        public override NodeCondition TryEntry()
        {
            float playerDistance = _playerInformation.ToPlayerDistance(_bossEnemyData.Position.Value);

            if (playerDistance < _nodeEntryLine) return NodeCondition.Success;

            return NodeCondition.Failure;
        }

        // プレイヤーの情報共有サービス
        private IPlayerInformationService _playerInformation;

        // ボスエネミーのデータ
        private BossEnemyData _bossEnemyData;

        // このノードのエントリー可能条件となる数値
        private readonly float _nodeEntryLine;
    }
    #endregion

    #region CountDownを行いカウントが0になったときEntryが可能となるDecoratorNode
    public class CountDownNode : DecoratorNode
    {
        public CountDownNode (ITreeNode child, int countStartValue) : base(child)
        {
            _startValue = countStartValue;
            _currentCount = _startValue;
        }

        public void CountDown() => _currentCount--;

        public void Reset() => _currentCount = _startValue;

        public override NodeCondition TryEntry()
        {
            if (_currentCount == 0)
            {
                Debug.Log("Entry成功");
                Reset();
                return NodeCondition.Success;
            }

            CountDown();
            return NodeCondition.Failure;
        }

        private int _currentCount;
        private readonly int _startValue;
    }
    #endregion
}
