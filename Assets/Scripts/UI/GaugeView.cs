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
    private Sequence _backgroundGaugeSeq;
    private Sequence _mainGaugeSeq;

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

        float mainAmount = _gauge.fillAmount;
        float amount = current / max;

        GaugeAnimation(mainAmount, amount);
    }

    private void GaugeAnimation(float mainAmount, float targetAmount)
    {
        _mainGaugeSeq?.Kill();
        _backgroundGaugeSeq?.Kill();

        // 回復（増える）
        if (mainAmount < targetAmount)
        {
            // 背景ゲージを先に瞬時に追いつかせる
            _backgroundGauge.fillAmount = targetAmount;

            // メインゲージをアニメーションで増やす
            _mainGaugeSeq = DOTween.Sequence()
                .Append(_gauge.DOFillAmount(targetAmount, _duration))
                .SetEase(_animEase)
                .SetLink(gameObject);

            return;
        }

        _gauge.fillAmount = targetAmount;

        // HPゲージを更新した後少し遅らせて背景のゲージを更新
        _backgroundGaugeSeq = DOTween.Sequence().
            Append(_backgroundGauge.DOFillAmount(targetAmount, _duration)).
            SetDelay(_delay).
            SetEase(_animEase).
            SetLink(gameObject);
    }
}
