using System;
using System.Collections.Generic;
using System.Linq;

public class SkillSelectPresenter:IDisposable
{
    public SkillSelectPresenter(
        SkillManager skillManager,
        ISkillSelectView view,
        IAttackStats stats)
    {
        _skillManager = skillManager;
        _view = view;
        _stats = stats;

        _view.OnSkillSelected += OnSkillSelected;
    }

    /// <summary> スキル選択UIを表示する </summary>
    public void Open(int candidateCount)
    {
        _currentSkills = _skillManager.GetSelectableSkills(candidateCount);

        var viewData = _currentSkills
            .Select(s => new SkillViewData(
                s.ID,
                s.Name,
                s.Explanation,
                s.Icon
            ))
            .ToList();

        _view.Show(viewData);
    }

    public void Dispose()
    {
        _view.OnSkillSelected -= OnSkillSelected;
    }

    private readonly SkillManager _skillManager;
    private readonly ISkillSelectView _view;
    private readonly IAttackStats _stats;
    private List<SkillBase> _currentSkills;

    /// <summary> ボタンが押されたときに呼ばれる </summary>
    private void OnSkillSelected(int skillId)
    {
        _skillManager.TryRegisterSkillId(skillId, _stats);  
        _view.Hide();
    }
}
