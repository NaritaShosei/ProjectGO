using DG.Tweening;
using UnityEngine;

public class LockOnMarkerView : MonoBehaviour
{
    public void Show(Vector3 worldPosition)
    {
        UpdatePosition(worldPosition);
        if (!_isVisible)
        {
            _isVisible = true;
            _canvasGroup.alpha = 0f;

            _fadeTween?.Kill();
            _fadeTween = _canvasGroup.DOFade(1f, _fadeDuration).SetLink(gameObject);
        }
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (!_isVisible) return;
        _isVisible = false;

        _fadeTween?.Kill();
        _fadeTween = _canvasGroup.DOFade(0f, _fadeDuration)
            .OnComplete(() => gameObject.SetActive(false))
            .SetLink(gameObject);
    }

    public void UpdatePosition(Vector3 worldPosition)
    {
        if (_mainCamera == null) return;

        Vector3 screenPos = _mainCamera.WorldToScreenPoint(worldPosition);

        // カメラ背後なら非表示
        if (screenPos.z < 0f)
        {
            _rectTransform.gameObject.SetActive(false);
            return;
        }

        _rectTransform.gameObject.SetActive(true);
        _rectTransform.position = screenPos;
    }

    public void SetCamera(Camera camera)
    {
        _mainCamera = camera;
    }

    [Header("アニメーション")]
    [SerializeField] private float _fadeDuration = 0.1f;
    [SerializeField] private float _scaleDuration = 0.5f;
    [SerializeField] private float _scale = 2f;

    [Header("参照")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private RectTransform _rectTransform; // このオブジェクトの RectTransform
    [SerializeField] private RectTransform _markerRoot; // マーカーのルート

    private Camera _mainCamera;
    private bool _isVisible;
    private Sequence _seq;
    private Tween _fadeTween;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    private void Start()
    {
        // スケールアニメーションのセットアップ
        _seq = DOTween.Sequence();

        _seq.Append(_markerRoot.DOScale(_scale, _scaleDuration))
            .Append(_markerRoot.DOScale(1f, _scaleDuration))
            .SetLoops(-1, LoopType.Restart)
            .SetEase(Ease.Linear)
            .SetLink(gameObject);
    }
}
