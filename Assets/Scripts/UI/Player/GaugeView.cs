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

        // barContainerだけ伸ばす
        UpdateBarWidth(max, initialMax);
        // HPの長さはwidthで直接制御
        AnimateHPGauge(current, max, initialMax);
    }

    [SerializeField] private RectTransform _barContainer;

    [SerializeField] private Image _gauge;

    [SerializeField] private Image _delayGauge;

    [SerializeField] private float _baseWidth;

    [SerializeField] private float _gaugeWidth;

    [SerializeField] private float _duration = 0.4f;

    [SerializeField] private float _delay = 0.4f;

    [SerializeField] private Ease _ease = Ease.Linear;

    [SerializeField] private float _sizeChangeDuration = 0.2f;

    [SerializeField] private Ease _sizeChangeEase = Ease.OutCubic;

    /// <summary>
    /// HPゲージTween
    /// </summary>
    private Sequence _mainSeq;

    /// <summary>
    /// 遅延ゲージTween
    /// </summary>
    private Sequence _delaySeq;

    /// <summary>
    /// バーの横幅変更Tween
    /// </summary>
    private Sequence _sizeChangeSeq;

    /// <summary>
    /// 最大HP増加時にバーの横幅を拡張する
    /// </summary>
    private void UpdateBarWidth(float max, float initialMax)
    {
        float ratio = max / initialMax;
        Vector2 size = _barContainer.sizeDelta;
        size.x = _baseWidth * ratio;

        _sizeChangeSeq?.Kill();

        _sizeChangeSeq = DOTween.Sequence()
            .Append(_barContainer.DOSizeDelta(size, _sizeChangeDuration))
            .SetEase(_sizeChangeEase)
            .SetLink(gameObject);
    }

    /// <summary>
    /// HPゲージのアニメーション処理
    /// </summary>
    private void AnimateHPGauge(float current, float max, float initialMax)
    {
        // HPの絶対的な長さ = gaugeWidth × (current / initialMax)
        float targetWidth = _gaugeWidth * (current / initialMax);
        float currentWidth = _gauge.rectTransform.sizeDelta.x;

        _mainSeq?.Kill();
        _delaySeq?.Kill();

        if (currentWidth < targetWidth)
        {
            // 回復
            _delayGauge.rectTransform.sizeDelta = new Vector2(targetWidth, _delayGauge.rectTransform.sizeDelta.y);
            _mainSeq = DOTween.Sequence()
                .Append(DOTween.To(
                    getter: () => _gauge.rectTransform.sizeDelta.x,
                    setter: x => _gauge.rectTransform.sizeDelta = new Vector2(x, _gauge.rectTransform.sizeDelta.y),
                    endValue: targetWidth,
                    duration: _duration))
                .SetEase(_ease)
                .SetLink(gameObject);
            return;
        }

        // ダメージ
        _gauge.rectTransform.sizeDelta = new Vector2(targetWidth, _gauge.rectTransform.sizeDelta.y);

        _delaySeq = DOTween.Sequence()
            .AppendInterval(_delay)
            .Append(DOTween.To(
                getter: () => _delayGauge.rectTransform.sizeDelta.x,
                setter: x => _delayGauge.rectTransform.sizeDelta = new Vector2(x, _delayGauge.rectTransform.sizeDelta.y),
                endValue: targetWidth,
                duration: _duration))
            .SetEase(_ease)
            .SetLink(gameObject);
    }

    private void OnValidate()
    {
        if (_barContainer != null)
        {
            _baseWidth = _barContainer.sizeDelta.x;
        }

        if (_gauge != null)
        {
            _gaugeWidth = _gauge.rectTransform.sizeDelta.x;
        }
    }
}
