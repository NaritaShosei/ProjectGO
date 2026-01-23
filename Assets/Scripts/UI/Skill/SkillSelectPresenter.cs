using System.Collections.Generic;
using System.Linq;

public class SkillSelectPresenter
{
    public SkillSelectPresenter(
      SkillManager skillManager,
      ISkillSelectView view)
    {
        _skillManager = skillManager;
        _view = view;

        _view.OnSkillSelected += OnSkillSelected;
    }

    /// <summary>
    /// スキル選択UIを表示する
    /// </summary>
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

    private readonly SkillManager _skillManager;
    private readonly ISkillSelectView _view;

    private List<SkillBase> _currentSkills;

    /// <summary>
    /// ボタンが押されたときに呼ばれる
    /// </summary>
    private void OnSkillSelected(int skillId)
    {
        _skillManager.TryRegisterSkillId(skillId);
        _view.Hide();
    }
}
