using UnityEngine;

public class LevelUpEffectPresenter
{
    public LevelUpEffectPresenter(LevelUpEffectView view, SkillManager skillManager)
    {
        _view = view;
        _skillManager = skillManager;

        _skillManager.OnApply += Play;
    }

    public void Play(StatSkillType statType)
    {
        _view.Play(statType);
    }

    public void Dispose()
    {
        _skillManager.OnApply -= Play;
    }

    private readonly LevelUpEffectView _view;
    private readonly SkillManager _skillManager;
}
