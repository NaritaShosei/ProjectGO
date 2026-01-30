using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;

public class ModeSelectView : MonoBehaviour
{
    public event Action OnInGameModeButton;
    public event Action OnBossModeButton;
    public event Action OnPracticeModeButton;
    public event Action OnBackButton;

    public void ShowThisPanel()
    {
        EventSystem.current.SetSelectedGameObject(_inGameModeButton.gameObject);
    }

    [Header("ボタン")]
    [SerializeField]
    private Button _inGameModeButton;
    [SerializeField]
    private Button _bossModeButton;
    [SerializeField]
    private Button _practiceModeButton;
    [SerializeField]
    private Button _backButton;

    private void Awake()
    {
        Debug.Assert(_inGameModeButton != null, "[ModeSelectView] _inGameModeButton が未設定です");
        Debug.Assert(_bossModeButton != null, "[ModeSelectView] _bossModeButton が未設定です");
        Debug.Assert(_practiceModeButton != null, "[ModeSelectView] _practiceModeButton が未設定です");
        Debug.Assert(_backButton != null, "[ModeSelectView] _backButton が未設定です");
    }

    private void Start()
    {
        // ボタンのクリックイベントにリスナーを追加
        _inGameModeButton.onClick.AddListener(() => OnInGameModeButton?.Invoke());
        _bossModeButton.onClick.AddListener(() => OnBossModeButton?.Invoke());
        _practiceModeButton.onClick.AddListener(() => OnPracticeModeButton?.Invoke());
        _backButton.onClick.AddListener(() => OnBackButton?.Invoke());

        ShowThisPanel();
    }

    private void OnDestroy()
    {
        // ボタンのクリックイベントからリスナーを削除
        _inGameModeButton.onClick.RemoveAllListeners();
        _bossModeButton.onClick.RemoveAllListeners();
        _practiceModeButton.onClick.RemoveAllListeners();
        _backButton.onClick.RemoveAllListeners();
    }
}
