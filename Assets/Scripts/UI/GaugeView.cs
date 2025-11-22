using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class GaugeView : MonoBehaviour
{
    [SerializeField] private Image _gauge;
    [SerializeField] private Image _backgroundGauge;
    [SerializeField] private float _duration = 0.5f;
    [SerializeField] private float _delay = 0.5f;
    [SerializeField] private Ease _animEase = Ease.Linear;
    private Sequence _gaugeSeq;

    public void Init(float current, float max)
    {
        if (max <= 0f)
        {
            Debug.LogWarning("最大値が0のため0除算が起きてしまいます。");
            return;
        }

        if (_gauge == null || _backgroundGauge == null)
        {
            Debug.LogError("Imageコンポーネントがアタッチされていません。");
            return;
        }

        var amount = current / max;

        _gauge.fillAmount = amount;
        _backgroundGauge.fillAmount = amount;
    }

    public void UpdateGauge(float current, float max)
    {
        if (max <= 0f)
        {
            Debug.LogWarning("最大値が0のため0除算が起きてしまいます。");
            return;
        }

        if (_gauge == null || _backgroundGauge == null)
        {
            Debug.LogError("Imageコンポーネントがアタッチされていません。");
            return;
        }

        var amount = current / max;
        _gauge.fillAmount = amount;

        // 再生中のアニメーションを中断
        _gaugeSeq?.Kill();

        // HPゲージを更新した後少し遅らせて背景のゲージを更新
        _gaugeSeq = DOTween.Sequence().
            Append(_backgroundGauge.DOFillAmount(amount, _duration)).
            SetDelay(_delay).
            SetEase(_animEase).
            SetLink(gameObject);
    }
}
