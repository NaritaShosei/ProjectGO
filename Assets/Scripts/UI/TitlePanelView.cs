using UnityEngine;
using UnityEngine.UI;
using System;

public class TitlePanelView : MonoBehaviour
{
    public event Action OnModeSelectButton;
    public event Action OnOptionButton;

    [SerializeField]
    private Button _modeSelectButton;
    [SerializeField]
    private Button _optionButton;

    private void Start()
    {
        _modeSelectButton.onClick.AddListener(() => OnModeSelectButton?.Invoke());
        _optionButton.onClick.AddListener(() => OnOptionButton?.Invoke());
    }
}
