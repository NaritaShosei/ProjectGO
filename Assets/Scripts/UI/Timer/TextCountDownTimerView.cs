using DG.Tweening;
using TMPro;
using UnityEngine;

public class TextCountDownTimerView : MonoBehaviour, IPhaseTimerView
{
    public void UpdateTimer(float current, float max)
    {
        if (!_emphasized && current <= _timerEmphasisThreshold)
        {
            Emphasis();
            _emphasized = true;
        }

        if (_timerText != null)
        {
            int min = Mathf.FloorToInt(current / 60f);
            int sec = Mathf.FloorToInt(current % 60f);
            int centSec = Mathf.FloorToInt(current * 100f) % 100;

            _timerText.text = $"{min:D2}:{sec:D2}:{centSec:D2}";
        }
    }

    public void ResetTimer()
    {
        _emphasized = false;
        if (_timerText != null)
        {
            _timerText.color = Color.white; 
        }
        if (_emphasisPanel != null)
        {
            _emphasisPanel.anchoredPosition = _initialPos; 
        }
    }

    [Header("テキストの参照")]
    [SerializeField] private TextMeshProUGUI _timerText;

    [Tooltip("残り時間がこの値以下になったら、演出を開始する")]
    [SerializeField] private float _timerEmphasisThreshold = 10f;

    [Tooltip("演出として動かすパネル")]
    [SerializeField] private RectTransform _emphasisPanel;
    [SerializeField] private Vector2 _targetPos;
    [SerializeField] private float _emphasisDuration = 0.5f;
    [SerializeField] private float _returnDelay = 0.5f;
    [SerializeField] private float _returnDuration = 0.5f;
    [SerializeField] private Ease _emphasisEase = Ease.InOutSine;

    [Tooltip("テキストの色")]
    [SerializeField] private Color _emphasisColor = Color.red;

    private Vector2 _initialPos;
    private Sequence _emphasisSequence;

    private bool _emphasized = false;

    private void Awake()
    {
        if (_emphasisPanel != null)
        {
            _initialPos = _emphasisPanel.anchoredPosition;
        }
    }

    private void Emphasis()
    {
        if (_emphasisPanel != null)
        {
            _emphasisSequence?.Kill();
            _emphasisSequence = DOTween.Sequence()
                .Append(_emphasisPanel.DOAnchorPos(_targetPos, _emphasisDuration).SetEase(_emphasisEase))
                .AppendInterval(_returnDelay)
                .Append(_emphasisPanel.DOAnchorPos(_initialPos, _returnDuration).SetEase(_emphasisEase));
        }
        if (_timerText != null)
        {
            // テキストの色を変える
            _timerText.color = _emphasisColor;
        }
    }
}
