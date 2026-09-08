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

        _worldPosition = viewModel.WorldPosition;

        _currentOffset = GetOffset(viewModel.IsWideSpread);
        _riseAmount = 0f;

        UpdateScreenPosition();

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

    [Header("表示位置ランダム幅（通常ダメージ）")]
    [SerializeField] private float _normalOffsetX = 20f;
    [SerializeField] private float _normalOffsetY = 15f;

    [Header("表示位置ランダム幅（雷追加ダメージ）")]
    [SerializeField] private float _wideOffsetX = 140f;
    [SerializeField] private float _wideOffsetY = 70f;

    [SerializeField, Range(0f, 0.95f), Tooltip("中心を避ける最小半径")]
    private float _wideMinRadius = 0.3f;

    [Header("色設定")]
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _weakColor = new Color(1f, 0.45f, 0f);

    private Tween _currentTween;
    private RectTransform _rectTransform;
    private Camera _mainCamera;
    private Vector3 _worldPosition;
    private Vector3 _currentOffset;
    private float _riseAmount;

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

    private void LateUpdate()
    {
        if (!gameObject.activeSelf) return;
        UpdateScreenPosition();
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

        seq.Append(DOTween.To(() => _riseAmount, v => _riseAmount = v, _popupDistance, _fadeDuration));
        seq.Join(_canvasGroup.DOFade(0f, _fadeDuration));

        seq.OnComplete(() =>
        {
            gameObject.SetActive(false);
            OnRelease?.Invoke(this);
        });
        _currentTween = seq;
    }

    private void UpdateScreenPosition()
    {
        if (_mainCamera == null) return;

        var screenPos = _mainCamera.WorldToScreenPoint(_worldPosition);
        screenPos.x += _currentOffset.x;
        screenPos.y += _currentOffset.y + _riseAmount;
        _rectTransform.position = screenPos;
    }


    private Vector3 GetOffset(bool isWideSpread)
    {
        if (!isWideSpread)
        {
            return new Vector3(
                UnityEngine.Random.Range(-_normalOffsetX, _normalOffsetX),
                UnityEngine.Random.Range(-_normalOffsetY, _normalOffsetY),
                0f);
        }

        float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        float radius = UnityEngine.Random.Range(_wideMinRadius, 1f); // 0〜1の正規化半径

        return new Vector3(
            Mathf.Cos(angle) * radius * _wideOffsetX,
            Mathf.Sin(angle) * radius * _wideOffsetY,
            0f);
    }
}
