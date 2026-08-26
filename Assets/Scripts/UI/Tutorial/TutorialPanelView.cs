using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// チュートリアルの1ページを表示するパネル。
/// 表示内容やページ送りの判断は State 側が担当する。
/// </summary>
public sealed class TutorialPanelView : MonoBehaviour
{
    public event Action OnNextRequested;

    public void Show(TutorialPage page, bool modal = true)
    {
        if (page == null)
            return;

        if (_titleText != null)
            _titleText.text = page.Title;

        if (_descriptionText != null)
            _descriptionText.text = page.Description;

        if (_illustration != null)
        {
            _illustration.sprite = page.Illustration;
            _illustration.gameObject.SetActive(page.Illustration != null);
        }

        ApplyLayout(modal);
        SetVisible(true, modal);
    }

    public void Hide() => SetVisible(false, false);

    public void SetProgress(string progress)
    {
        if (_descriptionText != null)
            _descriptionText.text = progress;
    }

    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private RectTransform _contentPanel;
    [SerializeField] private GameObject _backdrop;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _descriptionText;
    [SerializeField] private Image _illustration;
    [SerializeField] private Button _nextButton;

    private void Awake()
    {
        if (_nextButton != null)
            _nextButton.onClick.AddListener(HandleNextClicked);

        SetVisible(false, false);
    }

    private void OnDestroy()
    {
        if (_nextButton != null)
            _nextButton.onClick.RemoveListener(HandleNextClicked);
    }

    private void HandleNextClicked() => OnNextRequested?.Invoke();

    private void ApplyLayout(bool modal)
    {
        if (_backdrop != null)
            _backdrop.SetActive(modal);

        if (_nextButton != null)
            _nextButton.gameObject.SetActive(modal);

        if (_contentPanel == null)
            return;

        if (modal)
        {
            _contentPanel.anchorMin = new Vector2(0.18f, 0.2f);
            _contentPanel.anchorMax = new Vector2(0.82f, 0.8f);
        }
        else
        {
            // リアルタイム説明はプレイ画面を隠さないよう右端へ寄せる。
            _contentPanel.anchorMin = new Vector2(0.7f, 0.56f);
            _contentPanel.anchorMax = new Vector2(0.98f, 0.94f);
        }

        _contentPanel.anchoredPosition = Vector2.zero;
        _contentPanel.sizeDelta = Vector2.zero;
    }

    private void SetVisible(bool visible, bool blocksInput)
    {
        if (_canvasGroup == null)
        {
            gameObject.SetActive(visible);
            return;
        }

        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible && blocksInput;
        _canvasGroup.blocksRaycasts = visible && blocksInput;
    }
}

public enum TutorialTrigger
{
    BattleStarted,
    ModeChange,
    LockOn,
    FirstEnemyDefeated,
    WaveCleared,
}

[Serializable]
public sealed class TutorialPage
{
    public TutorialTrigger Trigger => _trigger;
    public string Title => _title;
    public string Description => _description;
    public Sprite Illustration => _illustration;
    public float Duration => Mathf.Max(0.1f, _duration);

    public TutorialPage(
        TutorialTrigger trigger,
        string title,
        string description,
        float duration = 6f)
    {
        _trigger = trigger;
        _title = title;
        _description = description;
        _duration = duration;
    }

    [SerializeField] private TutorialTrigger _trigger;
    [SerializeField] private string _title;
    [SerializeField, TextArea(3, 8)] private string _description;
    [SerializeField] private Sprite _illustration;
    [SerializeField, Min(0.1f)] private float _duration = 6f;
}
