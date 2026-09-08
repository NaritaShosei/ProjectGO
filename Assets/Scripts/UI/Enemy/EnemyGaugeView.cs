using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class EnemyGaugeView : MonoBehaviour, IPoolable
{
    // ── IPoolable ────────────────────────────────────────────

    /// <summary>プールから取り出された直後。ループタスクを再起動する。</summary>
    public void OnGet()
    {
        _cts = new CancellationTokenSource();
        PositionUpdateLoopAsync(_cts.Token).Forget();
    }

    /// <summary>プールへ返却される直前。表示をリセットしてループを停止する。</summary>
    public void OnRelease()
    {
        ResetView();
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    // ── Public API ───────────────────────────────────────────

    public void Initialize(Transform enemyTransform, Action<bool> onBehindCameraChanged)
    {
        _onBehindCameraChanged = onBehindCameraChanged;
        _linkEnemy = enemyTransform;

        _gaugeIcons = _gaugeIcons.OrderByDescending(icon => icon.FillThreshold).ToArray();

        if (ServiceLocator.TryGet(out CameraManager cameraManager))
        {
            _mainCamera = cameraManager.MainCamera;
        }

        // 初回生成時もプール返却時と同じ初期表示にそろえる。
        ResetView();
    }

    public void UpdateGauge(float current, float max)
    {
        float hpAmount = current / max;
        AnimateHPGauge(hpAmount);

        if (_gaugeIcons == null || _gaugeIcons.Length == 0) return;

        foreach (var iconData in _gaugeIcons)
        {
            if (hpAmount <= iconData.FillThreshold)
            {
                _gaugeIcon.sprite = iconData.IconSprite;
            }
        }
    }

    public void SetVisible(bool visible)
    {
        _isVisible = visible;
        ApplyVisibility();
    }

    public void ResetView()
    {
        if (_delaySequence != null && _delaySequence.IsActive())
            _delaySequence.Kill();

        _mainGauge.fillAmount = 1f;
        _delayGauge.fillAmount = 1f;

        if (_gaugeIcons != null || _gaugeIcons.Length != 0)
        {
            // 降順に並べ替えたアイコンの中で、最も高い閾値のアイコンをデフォルトとして設定
            _gaugeIcon.sprite = _gaugeIcons[0].IconSprite;
        }

        SetVisible(false);
    }

    // ── Private ──────────────────────────────────────────────

    [SerializeField] private RectTransform _barContainer;
    [SerializeField] private Image _mainGauge;
    [SerializeField] private Image _delayGauge;
    [SerializeField] private float _animationDuration = 0.4f;
    [SerializeField] private float _animationDelay = 0.4f;
    [SerializeField] private Ease _animationEase = Ease.Linear;

    [Serializable]
    private struct GaugeIconData
    {
        public float FillThreshold => _fillThreshold;
        public Sprite IconSprite => _iconSprite;

        [SerializeField] private float _fillThreshold;
        [SerializeField] private Sprite _iconSprite;
    }

    [SerializeField] private Image _gaugeIcon;
    [SerializeField] private GaugeIconData[] _gaugeIcons;

    private Sequence _delaySequence;
    private Transform _linkEnemy;
    private Camera _mainCamera;
    private bool _isVisible;
    private bool _isBehindCamera;
    private CancellationTokenSource _cts;

    private event Action<bool> _onBehindCameraChanged;

    private void AnimateHPGauge(float hpAmount)
    {
        _mainGauge.fillAmount = hpAmount;

        if (_delaySequence != null && _delaySequence.IsActive())
            _delaySequence.Kill();

        _delaySequence = DOTween.Sequence();
        _delaySequence.AppendInterval(_animationDelay);
        _delaySequence.Append(
            _delayGauge.DOFillAmount(hpAmount, _animationDuration).SetEase(_animationEase)
        );
    }

    private void ApplyVisibility()
    {
        _barContainer.gameObject.SetActive(_isVisible && !_isBehindCamera);
    }

    private async UniTaskVoid PositionUpdateLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await UniTask.Yield(PlayerLoopTiming.PostLateUpdate, ct);
            if (_linkEnemy == null || _mainCamera == null) continue;

            var screenPos = _mainCamera.WorldToScreenPoint(_linkEnemy.position);
            bool isBehind = screenPos.z < 0;

            if (_isBehindCamera != isBehind)
            {
                _isBehindCamera = isBehind;
                _onBehindCameraChanged?.Invoke(isBehind);
                ApplyVisibility();
            }

            if (_isBehindCamera) continue;

            _barContainer.position = screenPos;
        }
    }
}
