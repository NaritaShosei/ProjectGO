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
        public void Init(BossAttack attack, BossMove move)
        {
            _isInit = true;

            _attack = attack;
            _move = move;
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
            ITreeNode[] originChildrenNode = new ITreeNode[]
            {
                BuildArmorBreakActionTree(bossEnemyData),
                BuildAttackActionTree(bossEnemyData, attackDataRepositry, playerInformationService)
            };

            SelectorNode originNode = new(originChildrenNode);

            _behaviorController = new(originNode, new NodeRunningEndNotifier());

            _behaviorController.OnRunning();
        }

        public void HandleBossArmorBreak()
        {
            if (!_isInit) return;
            if (_breakArmorNode == null) return;

            _breakArmorNode.CanEntry();
            _behaviorController.OnRunning();
        }

        public void HandleDead()
        {
            _isInit = false;
        }

        // BehaviorTreeの操作Class
        private BehaviorController _behaviorController;

        private BossMove _move;

        // 初期化確認フラグ
        private bool _isInit = false;

        #region 装備破壊時の行動関連変数

        [Header("----------装備破壊時の行動関連変数----------")]

        private BreakArmorNode _breakArmorNode;

        #endregion

        #region 攻撃行動関連変数

        [Header("--------------攻撃行動関連変数--------------")]

        [Header("特別近接攻撃が可能となる通常攻撃回数")]
        [SerializeField] private int _normalAttackCount = 3;

        [Header("近距離攻撃判定の出る間合い")]
        [SerializeField] private float _bossTerritoryDistance = 2.0f;

        private BossAttack _attack;

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

        #endregion

        private ITreeNode BuildArmorBreakActionTree(BossEnemyData bossEnemyData)
        {
            return new NullAction(); // ToDo:Armor破壊時の処理
        }

        private ITreeNode BuildAttackActionTree(BossEnemyData bossEnemyData,
            BossEnemyAttackDataRepositry dataRepositry,
            IPlayerInformationService playerInformationService)
        {
            // 攻撃を行う際のシーケンスで動く3つのノードを生成
            TargetChaseAction targetChaseAction = new(playerInformationService.Player.GetTargetCenter(), 
                _move, bossEnemyData, playerInformationService);

            AttackAction attackNode = new AttackAction(_attack);

            LookAtTargetAction lookAtTargetAction = new(playerInformationService.Player.GetTargetCenter(), 
                _move, bossEnemyData, playerInformationService, _lookSpeed, _finishAngleThreshold); ;
            

            // 攻撃シーケンスを生成
            ITreeNode[] attackSequenceChildren = new ITreeNode[] { targetChaseAction, attackNode ,lookAtTargetAction };
            SequenceNode attackSequence = new(attackSequenceChildren);

            // 攻撃選択ノード
            _closeRangeNormalAttackSelect = new(attackSequence, bossEnemyData.CloseRangeNormalAttackFieldHolder, dataRepositry, attackNode, targetChaseAction);
            _closeRangeSpecialAttackSelect = new(attackSequence, bossEnemyData.CloseRangeFinishCountAttackFieldHolder, dataRepositry, attackNode, targetChaseAction);
            _longRangeAttackSelect = new(attackSequence, bossEnemyData.LongRangeAttackFieldHolder, dataRepositry, attackNode, targetChaseAction);

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
