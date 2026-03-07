using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class EnemyGaugeView : MonoBehaviour
{
    public void Initialize(Transform enemyTransform)
    {
        _linkEnemy = enemyTransform;
        _mainCamera = ServiceLocator.Get<CameraManager>().MainCamera;
    }

    public void UpdateGauge(float current, float max)
    {
        // HP割合（現在HP / 最大HP）
        float hpAmount = current / max;
        // HPゲージアニメーション
        AnimateHPGauge(hpAmount);
    }

    public void ResetView()
    {
        if (_delaySequence != null && _delaySequence.IsActive())
        {
            _delaySequence.Kill();
        }

        _mainGauge.fillAmount = 1f;
        _delayGauge.fillAmount = 1f;
    }

    [SerializeField] private RectTransform _barContainer;
    [SerializeField] private Image _mainGauge;
    [SerializeField] private Image _delayGauge;
    [SerializeField] private float _animationDuration = 0.4f;
    [SerializeField] private float _animationDelay = 0.4f;
    [SerializeField] private Ease _animationEase = Ease.Linear;
    [SerializeField] private float _verticalOffset = 50f;

    private Transform _linkEnemy;

    private Sequence _delaySequence;
    private Camera _mainCamera;

    private void AnimateHPGauge(float hpAmount)
    {
        _mainGauge.fillAmount = hpAmount;

        // 遅延ゲージのアニメーション
        if (_delaySequence != null && _delaySequence.IsActive())
        {
            _delaySequence.Kill();
        }

        _delaySequence = DOTween.Sequence();
        _delaySequence.AppendInterval(_animationDelay);
        _delaySequence.Append(_delayGauge.DOFillAmount(hpAmount, _animationDuration).SetEase(_animationEase));
    }

    private void Update()
    {
        if (_linkEnemy == null || _mainCamera == null) return;
        var screenPos = _mainCamera.WorldToScreenPoint(_linkEnemy.position);

        screenPos.y += _verticalOffset;

        _barContainer.position = screenPos;
    }
}
