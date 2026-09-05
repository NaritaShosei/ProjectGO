using UnityEngine;

/// <summary>
/// スキル選択タイマーの残り時間をゲージで表示する。
/// </summary>
public class GaugeCountDownTimerView : MonoBehaviour, IPhaseTimerView
{
    public void UpdateTimer(float current, float max)
    {
        if (!_hasMaximumGaugeWidth)
        {
            CacheMaximumGaugeWidth();
        }

        // 非表示中などで幅を取得できない場合は、次の更新を待つ。
        if (!_hasMaximumGaugeWidth)
        {
            return;
        }

        float remainingRatio = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        _timerGaugeRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            _maximumGaugeWidth * remainingRatio
        );
    }

    public void ResetTimer()
    {
        if (_hasMaximumGaugeWidth)
        {
            _timerGaugeRect.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                _maximumGaugeWidth
            );
        }
    }

    [SerializeField, Tooltip("残り時間に合わせて両端から中央へ縮むRectTransform")]
    private RectTransform _timerGaugeRect;

    private float _maximumGaugeWidth;
    private bool _hasMaximumGaugeWidth;

    private void Awake()
    {
        CacheMaximumGaugeWidth();
    }

    /// <summary>
    /// レイアウト計算後の幅を、タイマーが満タンのときのゲージ幅として保存する。
    /// </summary>
    private void CacheMaximumGaugeWidth()
    {
        if (_timerGaugeRect == null || _hasMaximumGaugeWidth)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        float gaugeWidth = _timerGaugeRect.rect.width;
        if (gaugeWidth <= 0f)
        {
            return;
        }

        _maximumGaugeWidth = gaugeWidth;
        _hasMaximumGaugeWidth = true;
    }
}
