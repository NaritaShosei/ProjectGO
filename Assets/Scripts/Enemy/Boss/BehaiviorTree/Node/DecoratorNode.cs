using BossEnemy.Data;
using UniRx;
using System;

namespace BossEnemy.BehaviorTree.Node.Decorator
{
    /// <summary> 通ったら子Nodeを実行して、通らなければFailureを返すNode </summary>
    public abstract class DecoratorNode : TreeNodeBase
    {
        public DecoratorNode(ITreeNode childNode)
        {
            _childNode = childNode;
        }

        public override void OnEnter()
        {
            if (_childNode.TryEntry() == NodeCondition.Success)
            {
                Controller.ChangeNode(_childNode);
                return;
            }

            Controller.StartSearch();
        }

        /// <summary> 子ノード </summary>
        protected ITreeNode _childNode = null;
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

        public void ArmorBreak() => _isArmorBreak = true;

        public override NodeCondition TryEntry()
        {
            if (_isArmorBreak)
            {
                _isArmorBreak = false;
                return NodeCondition.Success;
            }

            return NodeCondition.Failure;
        }

        private bool _isArmorBreak = false;
    }
    #endregion

    #region Playerとの距離に応じてEntryが可能となるDecoratorNode
    /// <summary> Playerとの距離に応じてEntryが可能となるDecoratorNode </summary>
    public class DependingOnDistanceFromPlayerNode : DecoratorNode
    {
        public DependingOnDistanceFromPlayerNode(ITreeNode child, IPlayerInformationService playerInformationService,
            BossEnemyData bossEnemyData, float nodeEntryLine, bool isEntryCloseDistance
            ) : base(child)
        {
            _playerInformation = playerInformationService;
        }

        public override NodeCondition TryEntry()
        {
            float distance = _playerInformation.ToPlayerDistance(_bossEnemyData.Position.Value);

            if (distance > _nodeEntryLine) return NodeCondition.Success;

            return NodeCondition.Failure;
        }

        // プレイヤーの情報共有サービス
        private IPlayerInformationService _playerInformation;

        // ボスエネミーのデータ
        private BossEnemyData _bossEnemyData;

        // このフラグがTrueだとEntry条件がPlayerとの距離がEntryLineより近いとEntry可能になる
        private readonly bool _isEntryCloseDistance;

        // このノードのエントリー可能条件となる数値
        private readonly float _nodeEntryLine;
    }
    #endregion

    #region 現在のBossEnemyのPhaseが最後のPhaseだった場合にEntry可能になるDecoratorNode
    /// <summary> 現在のBossEnemyのPhaseが最後のPhaseだった場合にEntry可能になるDecoratorNode </summary>
    public class LastPhaseNode : DecoratorNode
    {
        public LastPhaseNode(ITreeNode child, BossEnemyPhaseChanger phaseChanger) : base(child)
        {
            _phaseChanger = phaseChanger;
        }

        public override NodeCondition TryEntry()
        {
            if(_phaseChanger.IsAllPhaseFinish.Value) return NodeCondition.Success;

            return NodeCondition.Failure;
        }

        private readonly BossEnemyPhaseChanger _phaseChanger;

        private readonly int _lastPhaseNum;
    }
    #endregion
}
