using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ResultPanelView : MonoBehaviour
{
    public event Action TitleRequested;

    public int SMinimumScore => _sMinimumScore;
    public int AMinimumScore => _aMinimumScore;
    public int BMinimumScore => _bMinimumScore;

    public void SetBossClearTime(string value)
    {
        if (_clearTimeValue != null)
            _clearTimeValue.text = value;
    }

    public void SetRank(ResultRank rank)
    {
        if (_rankImage == null) return;
        _rankImage.sprite = rank switch
        {
            ResultRank.S => _sRankSprite,
            ResultRank.A => _aRankSprite,
            ResultRank.B => _bRankSprite,
            _ => _cRankSprite
        };
        _rankImage.preserveAspect = true;
        _rankImage.enabled = _rankImage.sprite != null;
    }

    public void SetLevel(string value)
    {
        if (_levelValue != null)
            _levelValue.text = value;
    }

    public void ShowPanel()
    {
        _root.SetActive(true);
        Canvas.ForceUpdateCanvases();
        if (_titleButton != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(_titleButton.gameObject);
    }

    public void HidePanel()
    {
        if (_root != null)
            _root.SetActive(false);
    }

    [SerializeField] private GameObject _root;
    [SerializeField] private TextMeshProUGUI _clearTimeValue;
    [SerializeField] private TextMeshProUGUI _levelValue;
    [SerializeField] private Button _titleButton;

    [Header("Rank Score Thresholds (S >= A >= B)")]
    [Tooltip("このスコア以上でS。残り時間とレベルから算出したスコアで判定します。")]
    [SerializeField, Min(0)] private int _sMinimumScore = 25000;
    [SerializeField, Min(0)] private int _aMinimumScore = 20000;
    [Tooltip("このスコア未満はCになります。")]
    [SerializeField, Min(0)] private int _bMinimumScore = 15000;

    [Header("Rank Images")]
    [SerializeField] private Image _rankImage;
    [SerializeField] private Sprite _sRankSprite;
    [SerializeField] private Sprite _aRankSprite;
    [SerializeField] private Sprite _bRankSprite;
    [SerializeField] private Sprite _cRankSprite;

    private void OnValidate()
    {
        _bMinimumScore = Mathf.Max(0, _bMinimumScore);
        _aMinimumScore = Mathf.Max(_bMinimumScore, _aMinimumScore);
        _sMinimumScore = Mathf.Max(_aMinimumScore, _sMinimumScore);
    }

    private void Awake()
    {
        if (_titleButton != null)
            _titleButton.onClick.AddListener(HandleTitleButtonClicked);

        HidePanel();
    }

    private void OnDestroy()
    {
        if (_titleButton != null)
            _titleButton.onClick.RemoveListener(HandleTitleButtonClicked);
    }

    private void HandleTitleButtonClicked() => TitleRequested?.Invoke();
}
