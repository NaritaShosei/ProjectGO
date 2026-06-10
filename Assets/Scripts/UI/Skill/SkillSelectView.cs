using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SkillSelectView : MonoBehaviour, ISkillSelectView
{
    /// <summary> スキルが選択されたときのイベント </summary>
    public event Action<int> OnSkillSelected;

    /// <summary> 現在選択されているスキルID。選択されていない場合は-1 </summary>
    public int CurrentSelectSkillId => _currentSelectSkillId;

    /// <summary> スキル選択UIを表示する。初期化もここで </summary>
    public void Show(List<SkillViewData> skills)
    {
        EventSystem.current.SetSelectedGameObject(_buttons[0].gameObject);

        _currentSelectSkillId = -1; // 初期化

        for (int i = 0; i < _buttons.Length; i++)
        {
            int index = i;

            if (i < skills.Count)
            {
                // スキル選択ボタンに表示用データと押された時のイベントを渡す
                _buttons[i].Setup(skills[index], () =>
                {
                    OnSkillSelected?.Invoke(skills[index].Id);
                });

                // マウスオーバーや選択されたときのイベントを登録
                _buttons[i].OnHighlighted += SetCurrentSelectSkill;
            }
            else
            {
                _buttons[i].gameObject.SetActive(false);
            }
        }

        _panel.gameObject.SetActive(true);
    }

    /// <summary> スキル選択UIを非表示にする </summary>
    public void Hide()
    {
        _panel.gameObject.SetActive(false);

        // イベントの解除
        foreach (var button in _buttons)
        {
            button.OnHighlighted -= SetCurrentSelectSkill;
        }
    }

    [SerializeField] private SkillSelectButton[] _buttons;
    [SerializeField] private GameObject _panel;

    private int _currentSelectSkillId = -1;

    private void SetCurrentSelectSkill(int id)
    {
        _currentSelectSkillId = id;
    }
}
