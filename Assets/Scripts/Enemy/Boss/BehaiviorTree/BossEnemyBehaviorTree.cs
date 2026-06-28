using UnityEngine;
using UniRx;

# region BossEnemy関連のusing
using BossEnemy.BehaviorTree.Node.ActionNode;
using BossEnemy.BehaviorTree.Node.DecoratorNode;
using BossEnemy.Data;
using BossEnemy.Data.Repositry;
using BossEnemy.Model.CoreLogic;
# endregion

namespace BossEnemy.BehaviorTree
{
    [CreateAssetMenu(fileName = "BossEnemyBehaviorTree", menuName = "BossEnemy/BehaviorTree")]
    public class BossEnemyBehaviorTree : ScriptableObject
    {
        public void Init(BossAttack attack, BossMove move, BossDown bossDown, AttackCoolTimer attackCoolTimer)
        {
            _isInit = true;

            _attack = attack;
            _move = move;
            _bossDown = bossDown;
            _attackCoolTimer = attackCoolTimer;
        }

        public void OnUpdate()
        {
            if (!_isInit) return;

            _behaviorController?.OnUpdate();
        }

        public void HandlePhaseChanged(BossEnemyData bossEnemyData,
            BossEnemyAttackDataRepositry attackDataRepositry,
            IPlayerInformationService playerInformationService)
        {
            if(_behaviorController != null) _behaviorController.StopRunning();

            // 共通で使いまわすNodeの初期化
            _lookAtTargetAction = new(playerInformationService.Player.GetTargetCenter(),
                _move, bossEnemyData, _lookSpeed, _finishAngleThreshold);

            ITreeNode[] originChildrenNode = new ITreeNode[]
            {
                BuildArmorBreakActionTree(bossEnemyData),
                BuildAttackActionTree(bossEnemyData, attackDataRepositry, playerInformationService)
            };

            SelectorNode originNode = new(originChildrenNode);

            _behaviorController = new(originNode, new NodeRunningEndNotifier());

            _behaviorController.OnRunning();
        }

        public void HandleBossArmorBreak(ArmorAttachmentPoint attachmentPointsType)
        {
            if (!_isInit || _downAction.IsDown) return;
            if (_breakArmorNode == null) return;

            _breakArmorNode.BreakArmor(attachmentPointsType);
            _behaviorController.OnRunning();
        }

        public void HandleDead()
        {
            _isInit = false;
        }

        LookAtTargetAction _lookAtTargetAction;

        // BehaviorTreeの操作Class
        private BehaviorController _behaviorController;

        // 初期化確認フラグ
        private bool _isInit = false;

        #region 装備破壊時の行動関連変数

        [Header("----------装備破壊時の行動関連変数----------")]

        [Header("片足破壊でダウンする時間")]
        [SerializeField] private float _oneLegBreakDownTime = 2.0f;

        [Header("両足破壊でダウンする時間")]
        [SerializeField] private float _allLegBreakDownTime = 2.0f;

        private BreakArmorNode _breakArmorNode;
        private DownAction _downAction;

        private BossDown _bossDown;
        #endregion

        #region 攻撃行動関連変数

        [Header("--------------攻撃行動関連変数--------------")]

        [Header("特別近接攻撃が可能となる通常攻撃回数")]
        [SerializeField] private int _normalAttackCount = 3;

        [Header("近距離攻撃判定の出る間合い")]
        [SerializeField] private float _bossTerritoryDistance = 2.0f;

        private BossAttack _attack;
        private AttackCoolTimer _attackCoolTimer;

        // 通常攻撃回数カウントノード
        private CountDownNode _normalAttackCountDownNode;

        // 各攻撃選択肢ノード
        private AttackSelect _closeRangeNormalAttackSelect;
        private AttackSelect _closeRangeSpecialAttackSelect;
        private AttackSelect _longRangeAttackSelect;

        #endregion

        #region 移動行動関連変数

        [Header("--------------移動行動関連変数--------------")]

        [Header("Bossの振り向きの速度")]
        [SerializeField] private float _lookSpeed = 3;

        [Header("振り向く際の振り向き終了角度との許される誤差")]
        [SerializeField] private float _finishAngleThreshold = 2.0f;

        private BossMove _move;

        #endregion

        private ITreeNode BuildArmorBreakActionTree(BossEnemyData bossEnemyData)
        {
            _downAction = new DownAction(bossEnemyData, _bossDown, _oneLegBreakDownTime, _allLegBreakDownTime);

            ITreeNode[] downSequenceChildren = new ITreeNode[]{ _downAction, _lookAtTargetAction };

            SequenceNode downSequence = new(downSequenceChildren);

            return _breakArmorNode = new(_downAction);
        }

        private ITreeNode BuildAttackActionTree(BossEnemyData bossEnemyData,
            BossEnemyAttackDataRepositry dataRepositry,
            IPlayerInformationService playerInformationService)
        {
            // ---攻撃シーケンスアクションノード↓---

            // 攻撃を行う際のシーケンスで動く3つのノードを生成
            TargetChaseAction targetChaseAction = new(playerInformationService.Player.GetTargetCenter(), 
                _move, bossEnemyData, playerInformationService);

            AttackAction attackNode = new AttackAction(_attack);

            _lookAtTargetAction = new(playerInformationService.Player.GetTargetCenter(), _move,
                bossEnemyData, _lookSpeed, _finishAngleThreshold);
            
            // 攻撃シーケンスを生成
            ITreeNode[] attackSequenceChildren = new ITreeNode[] { targetChaseAction, attackNode ,_lookAtTargetAction };
            SequenceNode attackSequence = new(attackSequenceChildren);

            // ---攻撃選択ノード↓-------------------

            // 1.近距離通常攻撃
            _closeRangeNormalAttackSelect = new(attackSequence, bossEnemyData.CloseRangeNormalAttackFieldHolder, 
                dataRepositry, _attackCoolTimer, attackNode, targetChaseAction);

            // 2.通常攻撃3回以上攻撃
            _closeRangeSpecialAttackSelect = new(attackSequence, bossEnemyData.CloseRangeFinishCountAttackFieldHolder, 
                dataRepositry, _attackCoolTimer, attackNode, targetChaseAction);

            // 3.遠距離攻撃
            _longRangeAttackSelect = new(attackSequence, bossEnemyData.LongRangeAttackFieldHolder,
                dataRepositry, _attackCoolTimer, attackNode, targetChaseAction);


            // カウントダウンを行いカウントが0になったときに近距離の特殊攻撃を行うノード
            _normalAttackCountDownNode = new(_closeRangeSpecialAttackSelect, _normalAttackCount);

            // ターゲットが近距離の際のノード
            ITreeNode[] closeAttackSelectorChild = new ITreeNode[] { _normalAttackCountDownNode, _closeRangeNormalAttackSelect };
            SelectorNode closeAttackSelector = new(closeAttackSelectorChild);
            
            // ターゲットの距離を見て近いときはEntryできるノード
            CloseToPlayerNode closeRangeTarget = new(closeAttackSelector, playerInformationService, bossEnemyData, _bossTerritoryDistance);

            // 上記のノード軍の最上位ノードを生成
            ITreeNode[] attackSelectorChild = new ITreeNode[] { closeRangeTarget, _longRangeAttackSelect };
            SelectorNode attackSelector = new(attackSelectorChild);

            return attackSelector;
        }
    }
}
