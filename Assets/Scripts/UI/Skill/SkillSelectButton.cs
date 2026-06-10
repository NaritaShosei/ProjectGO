using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillSelectButton : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    ISelectHandler,
    IDeselectHandler
{
    /// <summary> マウスカーソルが重なった際のイベント </summary>
    public event Action<int> OnHighlighted;

    public void Setup(SkillViewData viewData, Action onClick)
    {
        _skillId = viewData.Id; // スキルIDを保持しておく

        _nameText.text = viewData.Name;
        _explanationText.text = viewData.Explanation;
        _icon.sprite = viewData.Icon;

        _selectButton.onClick.RemoveAllListeners();
        if (onClick != null)
        {
            _selectButton.onClick.AddListener(onClick.Invoke);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        OnHovered(); // マウスが乗ったとき
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnUnhovered(); // マウスが離れたとき
    }

    public void OnDeselect(BaseEventData eventData)
    {
        OnDeselected(); // 方向キー等で選択が解除された時
    }

    public void OnSelect(BaseEventData eventData)
    {
        OnSelected(); // 方向キー等で選択された時
    }

    [SerializeField] private Button _selectButton;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _explanationText;
    [SerializeField] private Image _icon;

    private int _skillId; // スキルIDを保持しておく

    private void OnHovered()
    {
        Debug.Log("マウスが乗った");
        OnHighlighted?.Invoke(_skillId);
        Highlight(true);
    }

    private void OnUnhovered()
    {
        Debug.Log("マウスが離れた");
        Highlight(false);
    }

    private void OnSelected()
    {
        Debug.Log("方向キー等で選択された");
        OnHighlighted?.Invoke(_skillId);
        Highlight(true);
    }


    private void OnDeselected()
    {
        Debug.Log("方向キー等で選択が解除された");
        Highlight(false);
    }

    private void Highlight(bool isOn)
    {
        // 色変更 / 枠表示 / アニメーションなど
    }
}
