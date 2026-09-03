using TMPro;
using UnityEngine;

public class CountDownTimerView : MonoBehaviour, IPhaseTimerView
{
    public void UpdateTimer(float current, float max)
    {
        UpdateGauge(current, max);

        if (_timerText != null)
        {
            int min = Mathf.FloorToInt(current / 60f);
            int sec = Mathf.FloorToInt(current % 60f);
            int centSec = Mathf.FloorToInt(current * 100f) % 100;

            _timerText.text = $"{min:D2}:{sec:D2}:{centSec:D2}";
        }
    }
    [Header("テキストの参照")]
    [SerializeField] private TextMeshProUGUI _timerText;

    [Header("ゲージの参照")]
    [SerializeField, Tooltip("残り時間に合わせて両端から中央へ縮むRectTransform")]
    private RectTransform _timerGaugeRect;

    private float _maximumGaugeWidth;
    private bool _hasMaximumGaugeWidth;

    private void Awake()
    {
        CacheMaximumGaugeWidth();
    }

    /// <summary>
    /// ゲージの左右端を同じ割合で中央へ寄せ、残り時間を可視化する。
    /// </summary>
    private void UpdateGauge(float current, float max)
    {
        if (_timerGaugeRect == null)
        {
            return;
        }

        if (!_hasMaximumGaugeWidth)
        {
            CacheMaximumGaugeWidth();
        }

        // 非表示中などでレイアウト幅をまだ取得できない場合は、0幅で上書きせず次の更新を待つ。
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

    /// <summary>
    /// レイアウト計算後の表示幅を満タン時の幅として保存する。
    /// 非アクティブなUIではAwakeより先にタイマー更新が届くことがあるため、更新時にも呼び出す。
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
