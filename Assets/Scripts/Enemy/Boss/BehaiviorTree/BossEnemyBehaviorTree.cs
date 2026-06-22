using BossEnemy.BehaviorTree;
using BossEnemy.BehaviorTree.Node.Action;
using BossEnemy.BehaviorTree.Node.Decorator;
using BossEnemy.Data;
using UnityEngine;

[CreateAssetMenu(fileName = "BossEnemyBehaviorTree", menuName = "BossEnemy/BehaviorTree")]
public class BossEnemyBehaviorTree : ScriptableObject
{
    public void Init(BossEnemyData bossEnemyData, BossEnemyPhaseChanger bossEnemyPhaseChanger)
    {
        _isInit = true;

        ITreeNode[] originNode = new ITreeNode[]
        {
            GetHPGageBreakNode(bossEnemyData, bossEnemyPhaseChanger),
            GetArmorBreakActionNode(bossEnemyData),
            GetAttackActionNode(bossEnemyData)
        };

        SelectorNode selectorNode = new SelectorNode(originNode);
    }

    public void HandleBossArmorBreak()
    {
        if (_breakArmorNode == null) return;

        _breakArmorNode.ArmorBreak();
        _behaviorController.ForceRestartSearch();
    }

    private readonly BehaviorController _behaviorController;

    private BreakArmorNode _breakArmorNode;

    private bool _isInit = false;

    private ITreeNode GetHPGageBreakNode(BossEnemyData bossData, BossEnemyPhaseChanger bossEnemyPhaseChanger)
    {
        int entryLine = 0;

        // Bossが撃破された際に呼ばれるAction
        DefeatBoss defeatBossAction = new();

        // BossのPhaseが変わる際に呼ばれるAction
        PhaseChange phaseChangeAction = new();

        // 最後のPhaseが終了したか判定するDecorator
        LastPhaseNode lastPhaseDecorator = new(defeatBossAction, bossEnemyPhaseChanger);

        // HPが0になったときにボスのPhaseが残っているか確認するNodeを生成
        SelectorNode selectDeadOrNextPhase = new(new ITreeNode[]
        {
            lastPhaseDecorator, 
            defeatBossAction
        });

        // HPが0になった際に呼ばれるDecoratorNodeを生成
        BasedOnRemainingBossHPNode hpZeroDecorator = new(bossData, selectDeadOrNextPhase, entryLine, false);

        return hpZeroDecorator;
    }

    private ITreeNode GetArmorBreakActionNode(BossEnemyData bossEnemyData)
    {

        return null;
    }

    private ITreeNode GetAttackActionNode(BossEnemyData bossEnemyData)
    {
        return null;
    }
}
