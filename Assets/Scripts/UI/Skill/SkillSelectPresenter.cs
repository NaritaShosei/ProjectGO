using System;
using System.Collections.Generic;
using System.Linq;

public class SkillSelectPresenter : IDisposable
{
    public SkillSelectPresenter(
        SkillManager skillManager,
        ISkillSelectView view,
        IStatUpgradable stats)
    {
        _skillManager = skillManager;
        _view = view;
        _stats = stats;

        _view.OnSkillSelected += OnSkillSelected;
    }

    /// <summary> スキル選択UIを表示する </summary>
    public bool Open(int candidateCount)
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

        if (viewData.Count == 0)
        {
            return false;
        }

        _view.Show(viewData);
        return true;
    }

    public void Dispose()
    {
        _view.OnSkillSelected -= OnSkillSelected;
    }

    private readonly SkillManager _skillManager;
    private readonly ISkillSelectView _view;
    private readonly IStatUpgradable _stats;
    private List<SkillBase> _currentSkills;

    /// <summary> ボタンが押されたときに呼ばれる </summary>
    private void OnSkillSelected(int skillId)
    {
        _skillManager.TryRegisterSkillId(skillId, _stats);
        _view.Hide();
    }
}
