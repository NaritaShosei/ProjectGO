using UnityEngine;

[CreateAssetMenu(fileName = "EnemyPhaseData", menuName = "GameData/Phase/EnemyPhase")]
public class EnemyPhaseData : PhaseData
{
    public override bool IsComplete(PhaseContext context)
    {
        return context.RemainingEnemies == 0;
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
        // 時間経過の更新など
    }
}