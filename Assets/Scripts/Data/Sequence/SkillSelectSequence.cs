using System;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillSelectSequence", menuName = "GameData/Sequence/SkillSelectSequence")]

public class SkillSelectSequence : SequenceBase
{
    public override bool IsComplete(SequenceContext context)
    {
        return context.SkillSelected;
    }

    public override void OnSequenceStart(SequenceContext context)
    {
        if (context.SkillSelectView == null || context.SkillManager == null)
        {
            Debug.LogWarning("SkillUIManagerまたはSkillManagerがnullなので、スキル選択UIを表示できません");
            return;
        }

        if (_presenter == null)
        {
            _presenter = new SkillSelectPresenter(context.SkillManager, context.SkillSelectView);
        }

        _presenter.Open(context.SkillSelectCount);
    }

    public override void OnSequenceUpdate(SequenceContext context)
    {
        // 毎フレームの更新
    }

    [NonSerialized]
    private SkillSelectPresenter _presenter;
}