using UnityEngine;

[CreateAssetMenu(fileName = "BossBattleSequence", menuName = "GameData/Sequence/BossBattleSequence")]

public class BossBattleSequence : SequenceData
{
    public override bool IsComplete(PhaseContext context)
    {
        return context.BossDefeated;
    }

    public override void OnPhaseStart(PhaseContext context)
    {
        // 敵生成処理
        if (context.CurrentSpawnData != null)
        {
            var strategy = context.CurrentSpawnData.CreateStrategy(context.EnemyManager);
            strategy.Spawn();
        }
    }

    public override void OnPhaseUpdate(PhaseContext context)
    {
        // 毎フレームの更新処理
    }
}
