using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

        _emphasisSequence?.Kill();
        _emphasisImageSequence?.Kill();

        if (_timerText != null)
        {
            _timerText.color = _initialColor;
        }
        if (_emphasisPanel != null)
        {
            _emphasisPanel.anchoredPosition = _initialPos;
        }
        if (_emphasisImage != null)
        {
            Color color = _emphasisImage.color;
            color.a = 0f;
            _emphasisImage.color = color;
        }
    }

    [Header("テキストの参照")]
    [SerializeField] private TextMeshProUGUI _timerText;

    [Header("残り時間がこの値以下になったら、演出を開始する")]
    [SerializeField] private float _timerEmphasisThreshold = 10f;

    [Header("残り時間が一定以下時のテキストの色")]
    [SerializeField] private Color _emphasisColor = Color.red;

    [Header("演出として動かすパネル")]
    [SerializeField] private RectTransform _emphasisPanel;
    [SerializeField] private Vector2 _targetPos;
    [SerializeField] private float _emphasisDuration = 0.5f;
    [SerializeField] private float _returnDelay = 0.5f;
    [SerializeField] private float _returnDuration = 0.5f;
    [SerializeField] private Ease _emphasisEase = Ease.InOutSine;

    [Header("演出として点滅させるイメージ")]
    [SerializeField] private Image _emphasisImage;
    [SerializeField] private float _emphasisImageDuration = 0.5f;
    [SerializeField] private float _returnImageDelay = 0.5f;
    [SerializeField] private float _returnImageDuration = 0.5f;
    [SerializeField] private Ease _emphasisImageEase = Ease.InOutSine;

    private Color _initialColor;
    private Vector2 _initialPos;
    private Sequence _emphasisSequence;
    private Sequence _emphasisImageSequence;

    private bool _emphasized = false;

    private void Awake()
    {
        if (_emphasisPanel != null)
        {
            _initialPos = _emphasisPanel.anchoredPosition;
        }

        if (_timerText != null)
        {
            _initialColor = _timerText.color;
        }

        if (_emphasisImage != null)
        {
            Color color = _emphasisImage.color;
            color.a = 0f;
            _emphasisImage.color = color;
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

        if (_emphasisImage != null)
        {
            _emphasisImageSequence?.Kill();
            _emphasisImageSequence = DOTween.Sequence()
                .Append(_emphasisImage.DOFade(1f, _emphasisImageDuration).SetEase(_emphasisImageEase))
                .AppendInterval(_returnImageDelay)
                .Append(_emphasisImage.DOFade(0f, _returnImageDuration).SetEase(_emphasisImageEase)).
                SetLoops(-1);
        }

        if (_timerText != null)
        {
            // テキストの色を変える
            _timerText.color = _emphasisColor;
        }
    }
}
