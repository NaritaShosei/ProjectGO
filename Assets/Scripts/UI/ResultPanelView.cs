using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ResultPanelView : MonoBehaviour
{
    public event Action TitleRequested;

    public void SetBossClearTime(string value)
    {
        if (_clearTimeValue != null)
            _clearTimeValue.text = value;
    }

    public void SetScore(string value)
    {
        if (_scoreValue != null)
            _scoreValue.text = value;
    }

    public void SetLevel(string value)
    {
        if (_levelValue != null)
            _levelValue.text = value;
    }

    public void Show()
    {
        _root.SetActive(true);
        Canvas.ForceUpdateCanvases();
        if (_titleButton != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(_titleButton.gameObject);
    }

    public void Hide()
    {
        if (_root != null)
            _root.SetActive(false);
    }

    [SerializeField] private GameObject _root;
    [SerializeField] private TextMeshProUGUI _clearTimeValue;
    [SerializeField] private TextMeshProUGUI _scoreValue;
    [SerializeField] private TextMeshProUGUI _levelValue;
    [SerializeField] private Button _titleButton;

    private void Awake()
    {
        if (_titleButton != null)
            _titleButton.onClick.AddListener(HandleTitleButtonClicked);

        Hide();
    }

    private void OnDestroy()
    {
        if (_titleButton != null)
            _titleButton.onClick.RemoveListener(HandleTitleButtonClicked);
    }

    private void HandleTitleButtonClicked() => TitleRequested?.Invoke();
}
