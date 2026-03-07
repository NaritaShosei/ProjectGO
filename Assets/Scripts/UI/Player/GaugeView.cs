using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class GaugeView : MonoBehaviour
{
    /// <summary>
    /// HPゲージを更新する
    /// current : 現在HP
    /// max : 現在の最大HP
    /// initialMax : 初期最大HP（ゲージ拡張の基準）
    /// </summary>
    public void UpdateGauge(float current, float max, float initialMax)
    {
        if (initialMax <= 0f)
        {
            Debug.LogWarning("initialMax が 0 以下のため計算できません。");
            return;
        }

        // HP割合（現在HP / 最大HP）
        float hpAmount = current / max;

        // 最大HP増加によるバー幅更新
        UpdateBarWidth(max, initialMax);

        // HPゲージアニメーション
        AnimateHPGauge(hpAmount);
    }

    [SerializeField] private RectTransform _barContainer;

    [SerializeField] private Image _gauge;

    [SerializeField] private Image _delayGauge;

    [SerializeField] private float _baseWidth;

    [SerializeField] private float _duration = 0.4f;

    [SerializeField] private float _delay = 0.4f;

    [SerializeField] private Ease _ease = Ease.Linear;

    // 各ゲージとコントローラーの幅の差
    private float _gaugeWidthDiff;
    private float _delayGaugeWidthDiff;

    /// <summary>
    /// HPゲージTween
    /// </summary>
    private Sequence _mainSeq;

    /// <summary>
    /// 遅延ゲージTween
    /// </summary>
    private Sequence _delaySeq;

    /// <summary>
    /// 最大HP増加時にバーの横幅を拡張する
    /// </summary>
    private void UpdateBarWidth(float max, float initialMax)
    {
        float ratio = max / initialMax;

        Vector2 size = _barContainer.sizeDelta;
        size.x = _baseWidth * ratio;

        _barContainer.sizeDelta = size;

        // ゲージの幅も更新
        if (_gauge != null)
        {
            Vector2 gaugeSize = _gauge.rectTransform.sizeDelta;
            gaugeSize.x = size.x - _gaugeWidthDiff;
            _gauge.rectTransform.sizeDelta = gaugeSize;
        }

        if (_delayGauge != null)
        {
            Vector2 delayGaugeSize = _delayGauge.rectTransform.sizeDelta;
            delayGaugeSize.x = size.x - _delayGaugeWidthDiff;
            _delayGauge.rectTransform.sizeDelta = delayGaugeSize;
        }
    }

    /// <summary>
    /// HPゲージのアニメーション処理
    /// </summary>
    private void AnimateHPGauge(float targetAmount)
    {
        float currentAmount = _gauge.fillAmount;

        // 既存Tween停止
        _mainSeq?.Kill();
        _delaySeq?.Kill();

        // 回復処理
        if (currentAmount < targetAmount)
        {
            // 遅延ゲージを先に更新
            _delayGauge.fillAmount = targetAmount;

            _mainSeq = DOTween.Sequence()
                .Append(_gauge.DOFillAmount(targetAmount, _duration))
                .SetEase(_ease)
                .SetLink(gameObject);

            return;
        }

        // ダメージ処理
        _gauge.fillAmount = targetAmount;

        _delaySeq = DOTween.Sequence()
            .Append(_delayGauge.DOFillAmount(targetAmount, _duration))
            .SetDelay(_delay)
            .SetEase(_ease)
            .SetLink(gameObject);
    }

    private void OnValidate()
    {
        if (_barContainer != null)
        {
            _baseWidth = _barContainer.sizeDelta.x;

            if (_gauge != null)
            {
                var gaugeWidth = _gauge.rectTransform.sizeDelta.x;
                _gaugeWidthDiff = _baseWidth - gaugeWidth;
            }

            if (_delayGauge != null)
            {
                var delayGaugeWidth = _delayGauge.rectTransform.sizeDelta.x;
                _delayGaugeWidthDiff = _baseWidth - delayGaugeWidth;
            }
        }
    }
}
