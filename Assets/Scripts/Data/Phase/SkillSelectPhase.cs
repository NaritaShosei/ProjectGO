using UnityEngine;

public class SkillSelectPhase : PhaseData
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


public interface ISkillSelectUIManager
{
    public void Show();
}