using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

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
        _view.OnSkillHighlighted += OnSkillHighlighted;
    }

    /// <summary> スキル選択UIを表示する </summary>
    public bool Open(int candidateCount)
    {
        _currentSkills = _skillManager.GetSelectableSkills(candidateCount);
        _isSelected = false;
        _currentSkillId = -1; // 初期化

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
        // 現在ハイライト中のスキルIDを優先して登録する
        if (_currentSkillId != -1)
        {
            _view.SkillSelection(_currentSkillId);
        }
        // そうでなければ、選択肢の最初のスキルを登録する
        else if (_currentSkills != null && _currentSkills.Count > 0)
        {
            _view.SkillSelection(_currentSkills[0].ID);
        }
    }

    public void Dispose()
    {
        _view.OnSkillSelected -= OnSkillSelected;
        _view.OnSkillHighlighted -= OnSkillHighlighted;
    }

    private readonly SkillManager _skillManager;
    private readonly ISkillSelectView _view;
    private readonly IPlayerStats _stats;
    private int _currentSkillId = -1;
    private List<SkillBase> _currentSkills;
    private bool _isSelected = false;

    /// <summary> ボタンが押されたときに呼ばれる </summary>
    private void OnSkillSelected(int skillId)
    {
        SelectSkill(skillId);
    }

    /// <summary> スキルを登録して選択完了とする </summary>
    private void SelectSkill(int skillId)
    {
        if (_isSelected) return;

        _isSelected = true;
        _skillManager.TryRegisterSkillId(skillId, _stats);
        _view.Hide();
    }

    /// <summary> ハイライトが切り替わったときに呼ばれる </summary>
    private void OnSkillHighlighted(int skillId)
    {
        _view.UnhighlightButton(_currentSkillId); // 前のボタンのハイライトを停止
        _currentSkillId = skillId;
    }
}