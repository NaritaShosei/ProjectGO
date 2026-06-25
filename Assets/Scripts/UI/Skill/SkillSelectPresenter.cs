using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// スキル選択UIとスキル管理処理をつなぐPresenter。
/// Viewからは「どのスキルが選ばれたか」だけを受け取り、
/// 実際のスキル登録やUIを閉じる処理をここで行う。
/// </summary>
public class SkillSelectPresenter : IDisposable
{
    /// <summary>
    /// スキル選択Presenterを生成し、Viewからのイベント購読を開始する。
    /// </summary>
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

    /// <summary>
    /// スキル候補を取得して、スキル選択UIを表示する。
    /// 候補が1件もない場合はUIを開かず、falseを返す。
    /// </summary>
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

    /// <summary>
    /// 時間切れの際に呼ばれるスキル自動選択。
    /// 現在ハイライト中のスキルがあればそれを優先し、なければ候補の先頭を選択する。
    /// 自動選択は演出完了を待たず、スキル獲得とUI終了を確実に行う。
    /// </summary>
    public void AutoSelect()
    {
        // 現在ハイライト中のスキルIDを優先して登録する
        if (_currentSkillId != -1)
        {
            SelectSkill(_currentSkillId);
        }
        // そうでなければ、選択肢の最初のスキルを登録する
        else if (_currentSkills != null && _currentSkills.Count > 0)
        {
            SelectSkill(_currentSkills[0].ID);
        }
    }

    /// <summary>
    /// Viewイベントの購読を解除する。
    /// Presenterの寿命が終わったあとにViewから呼ばれ続けないようにする。
    /// </summary>
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

    /// <summary>
    /// ボタンのクリック演出が終わり、スキル選択が確定したときに呼ばれる。
    /// </summary>
    private void OnSkillSelected(int skillId)
    {
        SelectSkill(skillId);
    }

    /// <summary>
    /// スキルを登録して選択完了とする。
    /// クリック連打や自動選択との競合で二重登録されないよう、最初の1回だけ処理する。
    /// </summary>
    private void SelectSkill(int skillId)
    {
        if (_isSelected) return;

        _isSelected = true;
        _skillManager.TryRegisterSkillId(skillId, _stats);
        _view.Hide();
    }

    /// <summary>
    /// ハイライトが切り替わったときに呼ばれる。
    /// ここでは演出制御は行わず、自動選択用に現在のスキルIDだけを記録する。
    /// </summary>
    private void OnSkillHighlighted(int skillId)
    {
        _currentSkillId = skillId;
    }
}
