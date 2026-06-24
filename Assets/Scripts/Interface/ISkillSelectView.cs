using System;
using System.Collections.Generic;
using UnityEngine;

public interface ISkillSelectView
{
    public event Action<int> OnSkillSelected;
    public event Action<int> OnSkillHighlighted;
    public void Show(List<SkillViewData> skills);
    public void Hide();
    public void UnhighlightButton(int skillId);
    public void SkillSelection(int skillId);
}

public readonly struct SkillViewData
{
    public readonly int Id;
    public readonly string Name;
    public readonly string Explanation;
    public readonly Sprite Icon;

    public SkillViewData(int id, string name, string explanation, Sprite icon)
    {
        Id = id;
        Name = name;
        Explanation = explanation;
        Icon = icon;
    }
}

