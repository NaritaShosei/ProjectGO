using System;
using System.Collections.Generic;
using System.Linq;

public class SkillSelectPresenter : IDisposable
{
    public SkillSelectPresenter(
        SkillManager skillManager,
        ISkillSelectView view,
        IPlayerStats stats)
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

        _isSelected = false; // スキル選択が開始されたので、選択フラグをリセット

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

    /// <summary> 時間切れの際に呼ばれるスキル自動選択 </summary>
    public void AutoSelect()
    {
        // 現在選択されているスキルIDを優先して登録する
        if (_view.CurrentSelectSkillId != -1)
        {
            SelectSkill(_view.CurrentSelectSkillId);
        }

        // そうでなければ、選択肢の最初のスキルを登録する
        else if (_currentSkills != null && _currentSkills.Count > 0)
        {
            SelectSkill(_currentSkills[0].ID);
        }
    }

    public void Dispose()
    {
        _view.OnSkillSelected -= OnSkillSelected;
    }

    private readonly SkillManager _skillManager;
    private readonly ISkillSelectView _view;
    private readonly IPlayerStats _stats;
    private List<SkillBase> _currentSkills;

    private bool _isSelected = false; // スキルが選択されたかどうかのフラグ

    /// <summary> ボタンが押されたときに呼ばれる </summary>
    private void OnSkillSelected(int skillId)
    {
        SelectSkill(skillId);
    }

    /// <summary> スキルが選択されたときに呼ばれる </summary>
    private void SelectSkill(int skillId)
    {
        if (_isSelected)
        {
            return; // すでにスキルが選択されている場合は何もしない
        }

        _isSelected = true; // スキルが選択されたことを記録

        _skillManager.TryRegisterSkillId(skillId, _stats);
        _view.Hide();
    }
}
