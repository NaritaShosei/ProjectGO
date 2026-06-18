using UnityEngine;

[CreateAssetMenu(fileName = "EnemySequence", menuName = "GameData/Sequence/EnemySequence")]
public class EnemySequence : SequenceBase
{
    public override bool IsComplete(SequenceContext context)
    {
        return context.RemainingEnemies == 0;
    }

    public override void OnSequenceStart(SequenceContext context)
    {
        if (context.InputHandler != null)
        {
            context.InputHandler.EnableInput(true);
        }

       // if(context.WaveController == null)
        {
            Debug.LogError("[EnemySequence] WaveController が null です");
            return;
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
        // 時間経過の更新など
    }
}
