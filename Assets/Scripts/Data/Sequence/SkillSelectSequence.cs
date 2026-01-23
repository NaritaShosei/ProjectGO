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
        if (context.SkillUIManager == null)
        {
            Debug.LogWarning("SkillUIManagerがnullなので、スキル選択UIを表示できません");
            return;
        }

        _presenter = new SkillSelectPresenter(context.SkillManager,context.SkillUIManager);

        _presenter.Open(context.SkillSelectCount);
    }

    public override void OnPhaseUpdate(PhaseContext context)
    {
        // 毎フレームの更新
    }

    private SkillSelectPresenter _presenter;
}