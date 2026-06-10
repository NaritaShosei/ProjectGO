using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SkillSelectView : MonoBehaviour, ISkillSelectView
{
    public event Action<int> OnSkillSelected;

    public void Show(List<SkillViewData> skills)
    {
        EventSystem.current.SetSelectedGameObject(_buttons[0].gameObject.gameObject);

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

                _buttons[i].OnHighlighted += SetCurrentSelectSkill;
            }
            else
            {
                _buttons[i].gameObject.SetActive(false);
            }
        }

        _panel.gameObject.SetActive(true);
    }

    public void Hide()
    {
        _panel.gameObject.SetActive(false);
    }

    [SerializeField] private SkillSelectButton[] _buttons;
    [SerializeField] private GameObject _panel;

    private void SetCurrentSelectSkill(int id)
    {

    }
}
