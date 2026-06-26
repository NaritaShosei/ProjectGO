using DG.Tweening;
using System;
using TMPro;
using UnityEngine;

/// <summary>
/// ダメージポップアップUIの表示を管理するView。
/// </summary>
public class DamagePopupView : MonoBehaviour, IDamagePopupView, IPoolable
{
    public event Action<IDamagePopupView> OnRelease;

    // ── IPoolable ────────────────────────────────────────────

    /// <summary>プールから取り出された直後。透明度だけリセットする。</summary>
    public void OnGet()
    {
        _canvasGroup.alpha = 1f;
    }

    /// <summary>プールへ返却される直前。Tweenを停止して初期状態へ戻す。</summary>
    void IPoolable.OnRelease()
    {
        _currentTween?.Kill(false);
        _currentTween = null;
        _canvasGroup.alpha = 0f;
        _criticalObj.SetActive(false);
    }

    // ── IDamagePopupView ─────────────────────────────────────

    public void ShowDamage(DamagePopupViewModel viewModel)
    {
        // OnGet で alpha がリセットされているため追加リセット不要
        _criticalObj.SetActive(false);

        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                gameObject.SetActive(false);
                OnRelease?.Invoke(this);
                return;
            }
        }

        gameObject.SetActive(true);

        _currentTween?.Kill(false);
        _currentTween = null;

        _canvasGroup.alpha = 1f;
        _damageText.text = viewModel.Damage.ToString();
        _damageText.color = viewModel.TextColor ?? (viewModel.IsWeakPoint ? _weakColor : _normalColor);
        _criticalObj.SetActive(viewModel.IsCritical);

        var screenPos = _mainCamera.WorldToScreenPoint(viewModel.WorldPosition);
        screenPos.x += UnityEngine.Random.Range(-_randomOffsetX, _randomOffsetX);
        screenPos.y += UnityEngine.Random.Range(-_randomOffsetY, _randomOffsetY);
        _rectTransform.position = screenPos;

        PlayAnimation();
    }

    // ── Inspector ────────────────────────────────────────────

    [SerializeField] private TextMeshProUGUI _damageText;
    [SerializeField] private GameObject _criticalObj;
    [SerializeField] private CanvasGroup _canvasGroup;

    [Header("表示設定")]
    [SerializeField] private float _peakScale = 1.2f;
    [SerializeField] private float _popDuration = 0.1f;
    [SerializeField] private float _settleDuration = 0.1f;
    [SerializeField] private float _endScale = 1f;
    [SerializeField] private float _lifeTime = 1.5f;
    [SerializeField] private float _fadeDuration = 0.2f;
    [SerializeField] private float _popupDistance = 10f;
    [SerializeField] private float _randomOffsetX = 15f;
    [SerializeField] private float _randomOffsetY = 10f;

    [Header("色設定")]
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _weakColor = new Color(1f, 0.45f, 0f);

    // ── Private ──────────────────────────────────────────────

    private Tween _currentTween;
    private RectTransform _rectTransform;
    private Camera _mainCamera;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup.alpha = 0f;
    }

    private void Start()
    {
        if (ServiceLocator.TryGet(out CameraManager cameraManager))
        {
            _mainCamera = cameraManager.MainCamera;
        }
    }

    private void OnDisable()
    {
        _currentTween?.Kill(false);
        _currentTween = null;
    }

    private void PlayAnimation()
    {
        Sequence seq = DOTween.Sequence();

        seq.Append(_rectTransform.DOScale(_peakScale, _popDuration).SetEase(Ease.OutBack));
        seq.Append(_rectTransform.DOScale(_endScale, _settleDuration));

        float wait = Mathf.Max(0f, _lifeTime - _fadeDuration);
        seq.AppendInterval(wait);

        seq.Append(_rectTransform.DOMoveY(_rectTransform.position.y + _popupDistance, _fadeDuration));
        seq.Join(_canvasGroup.DOFade(0f, _fadeDuration));

        seq.OnComplete(() =>
        {
            gameObject.SetActive(false);
            OnRelease?.Invoke(this);
        });
        _currentTween = seq;
    }
}
