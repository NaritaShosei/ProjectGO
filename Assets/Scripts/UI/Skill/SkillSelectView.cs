using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SkillSelectView : MonoBehaviour, ISkillSelectView
{
    /// <summary> スキルが選択されたときのイベント </summary>
    public event Action<int> OnSkillSelected;

    /// <summary> スキルがハイライトされたときのイベント </summary>
    public event Action<int> OnSkillHighlighted;

    /// <summary> スキル選択UIを表示する。初期化もここで </summary>
    public void Show(List<SkillViewData> skills)
    {
        _buttonMap.Clear();

        for (int i = 0; i < _buttonArray.Length; i++)
        {
            int index = i;

            if (i < skills.Count)
            {
                _buttonArray[i].gameObject.SetActive(true);

                // スキル選択ボタンに表示用データと押された時のイベントを渡す
                _buttonArray[i].Setup(
                    skills[index],
                    () => OnSkillSelected?.Invoke(skills[index].Id)
                );

                // ハイライトイベントを登録
                _buttonArray[i].OnHighlighted += OnButtonHighlighted;

                // IDとボタンを紐付け
                _buttonMap[skills[i].Id] = _buttonArray[i];
            }
            else
            {
                _buttonArray[i].gameObject.SetActive(false);
            }
        }

        _panel.gameObject.SetActive(true);

        // 最初のスキルをデフォルトハイライト
        if (skills.Count > 0)
        {
            _buttonMap[skills[0].Id].ForceHighlight();
            EventSystem.current.SetSelectedGameObject(_buttonMap[skills[0].Id].gameObject);
        }
    }

    /// <summary> スキル選択UIを非表示にする </summary>
    public void Hide()
    {
        _panel.gameObject.SetActive(false);

        // イベントの解除
        foreach (var button in _buttonMap.Values)
        {
            button.OnHighlighted -= OnButtonHighlighted;
        }

        _buttonMap.Clear();
    }

    /// <summary> 指定したスキルIDのボタンのハイライトを停止する </summary>
    public void UnhighlightButton(int skillId)
    {
        if (skillId == -1) return;
        if (_buttonMap.TryGetValue(skillId, out var button))
        {
            button.ForceUnhighlight();
        }
    }

    /// <summary> クリックイベント /// </summary>
    public void SkillSelection(int skillId)
    {
        if (skillId == -1)return;
        if(_buttonMap.TryGetValue(skillId, out var button))
        {
            button.SkillSelection(() => OnSkillSelected?.Invoke(skillId));
        }
    }

    [SerializeField] private SkillSelectButton[] _buttonArray;
    [SerializeField] private GameObject _panel;

    // skillId → ボタンのマッピング
    private readonly Dictionary<int, SkillSelectButton> _buttonMap = new();

    private void OnButtonHighlighted(int skillId)
    {
        OnSkillHighlighted?.Invoke(skillId);
    }
}