using UnityEngine;

[CreateAssetMenu(fileName = "SkillSelectSequence", menuName = "GameData/Sequence/SkillSelectSequence")]

public class SkillSelectSequence : SequenceBase
{
    public override bool IsComplete(PhaseContext context)
    {
        return context.SkillSelected;
    }

    public override void OnPhaseStart(PhaseContext context)
    {
        context.SkillUIManager.Show();
    }

    public override void OnPhaseUpdate(PhaseContext context)
    {
        // 毎フレームの更新
    }
}