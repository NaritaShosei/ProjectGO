using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class GameOverView : MonoBehaviour, IGameOverView
{
    public event Action OnHealthDepletedShown;
    public event Action OnTimeExpiredShown;
    public event Action OnHidden;
    public event Action TitleRequested;

    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TMP_Text _messageText;
    [SerializeField] private Button _titleButton;

    public void Show(string message, GameOverReason reason)
    {
        if (_messageText != null) _messageText.text = message;
        SetVisible(true);

        // Connect death animation and camera direction to these reason-specific hooks.
        switch (reason)
        {
            case GameOverReason.PlayerHealthDepleted:
                OnHealthDepletedShown?.Invoke();
                break;
            case GameOverReason.TimeExpired:
                OnTimeExpiredShown?.Invoke();
                break;
        }
    }

    public void Hide()
    {
        SetVisible(false);
        OnHidden?.Invoke();
    }

    private void Awake()
    {
        if (_titleButton != null)
            _titleButton.onClick.AddListener(HandleTitleButtonClicked);

        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (_titleButton != null)
            _titleButton.onClick.RemoveListener(HandleTitleButtonClicked);
    }

    private void HandleTitleButtonClicked() => TitleRequested?.Invoke();

    private void SetVisible(bool visible)
    {
        if (_canvasGroup == null)
        {
            gameObject.SetActive(visible);
            return;
        }

        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
    }

}
