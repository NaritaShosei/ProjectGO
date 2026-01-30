using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;

public class TitlePanelView : MonoBehaviour
{
    public event Action OnModeSelectButton;
    public event Action OnOptionButton;

    public void ShowDhisPanel()
    {
        EventSystem.current.SetSelectedGameObject(_modeSelectButton.gameObject);
    }

    [SerializeField]
    private Button _modeSelectButton;
    [SerializeField]
    private Button _optionButton;

    private void Start()
    {
        // ボタンのクリックイベントにリスナーを追加
        _modeSelectButton.onClick.AddListener(() => OnModeSelectButton?.Invoke());
        _optionButton.onClick.AddListener(() => OnOptionButton?.Invoke());

        ShowDhisPanel();
    }

    private void OnDestroy()
    {
        // ボタンのクリックイベントからリスナーを削除
        _modeSelectButton.onClick.RemoveAllListeners();
        _optionButton.onClick.RemoveAllListeners();
    }
}
