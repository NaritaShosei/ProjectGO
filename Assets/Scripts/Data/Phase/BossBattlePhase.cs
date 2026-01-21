using UnityEngine;

[CreateAssetMenu(fileName = "BossBattlePhase", menuName = "GameData/Phase/BossBattlePhase")]

public class BossBattlePhase : PhaseData
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
