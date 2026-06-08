using UnityEngine;

[CreateAssetMenu(fileName = "EnemySequence", menuName = "GameData/Sequence/EnemySequence")]
public class EnemySequence : SequenceBase
{
    public override bool IsComplete(SequenceContext context)
    {
        return context.WaveController.IsComplete;
    }

    public override void OnSequenceStart(SequenceContext context)
    {
        if (context.InputHandler != null)
        {
            context.InputHandler.EnableInput(true);
        }

        //敵の生成処理
        context.WaveController.StartWave(context.CurrentWaveData);
    }

    public override void OnSequenceUpdate(SequenceContext context)
    {
        // 時間経過の更新など
    }
}
