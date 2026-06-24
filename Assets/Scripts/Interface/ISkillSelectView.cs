using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// スキル選択ViewがPresenterへ公開するインターフェース。
/// Presenterは具体的なUI実装を知らず、このインターフェース越しに表示/非表示とイベント購読だけを行う。
/// </summary>
public interface ISkillSelectView
{
    /// <summary>
    /// スキル選択が確定したときに通知される。
    /// 引数は選択されたスキルID。
    /// </summary>
    public event Action<int> OnSkillSelected;

    /// <summary>
    /// ハイライト中のスキルが切り替わったときに通知される。
    /// 自動選択時に現在の候補を選ぶために使う。
    /// </summary>
    public event Action<int> OnSkillHighlighted;

    /// <summary>
    /// 指定されたスキル候補を使ってスキル選択UIを表示する。
    /// </summary>
    public void Show(List<SkillViewData> skills);

    /// <summary>
    /// スキル選択UIを非表示にする。
    /// </summary>
    public void Hide();
}

/// <summary>
/// Viewへ渡すためのスキル表示データ。
/// スキル本体ではなく、UI表示に必要な情報だけを持たせる。
/// </summary>
public readonly struct SkillViewData
{
    /// <summary> スキルID。選択確定時にPresenterへ返す値。 </summary>
    public readonly int Id;

    /// <summary> UIに表示するスキル名。 </summary>
    public readonly string Name;

    /// <summary> UIに表示するスキル説明文。 </summary>
    public readonly string Explanation;

    /// <summary> UIに表示するスキルアイコン。 </summary>
    public readonly Sprite Icon;

    /// <summary>
    /// スキル表示データを生成する。
    /// </summary>
    public SkillViewData(int id, string name, string explanation, Sprite icon)
    {
        Id = id;
        Name = name;
        Explanation = explanation;
        Icon = icon;
    }
}

