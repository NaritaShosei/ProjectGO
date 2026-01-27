using UnityEngine;

[CreateAssetMenu(fileName = "BossBattleSequence", menuName = "GameData/Sequence/BossBattleSequence")]

public class BossBattleSequence : SequenceBase
{
    public override bool IsComplete(SequenceContext context)
    {
        return context.BossDefeated;
    }

    public override void OnSequenceStart(SequenceContext context)
    {
        if (context.InputHandler != null)
        {
            context.InputHandler.EnableInput(true);
        }

        // 敵生成処理
        if (context.CurrentSpawnData != null)
        {
            var strategy = context.CurrentSpawnData.CreateStrategy(context.EnemyManager);
            strategy.Spawn();
        }
    }

    public override void OnSequenceUpdate(SequenceContext context)
    {
        // 毎フレームの更新処理
    }
}
