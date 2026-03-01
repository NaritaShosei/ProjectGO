using DG.Tweening;
using System;
using TMPro;
using UnityEngine;

public class DamagePopupView : MonoBehaviour,IDamagePopupView
{
    [SerializeField] private TextMeshProUGUI _damageText;
    [SerializeField] private GameObject _weakPointObj;
    [SerializeField] private GameObject _criticalObj;
    [SerializeField] private CanvasGroup _canvasGroup;

    [Header("表示設定")]
    [SerializeField] private float _lifeTime = 1.5f;//消滅までの時間
    [SerializeField] private float _fadeDuration = 0.2f;//フェードの時間
    [SerializeField] private float _popupDistance = 10f;

    [Header("色設定")]
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _weakColor = new Color(1f, 0.45f, 0f);

    public event Action<IDamagePopupView> OnRelease;

    private Tween _currentTween;
    private RectTransform _rectTransform;
    private Camera _mainCamera;
    private void Awake()
    {
        _mainCamera = Camera.main;
        _rectTransform = GetComponent<RectTransform>();

        _canvasGroup.alpha = 0f;
    }

    public void ShowDamage(DamagePopupViewModel viewModel)
    {
        gameObject.SetActive(true);

        //前回のアニメーションを停止
        _currentTween?.Kill();

        //透明度の初期化
        _canvasGroup.alpha = 1f;

        //テキスト設定
        _damageText.text = viewModel.Damage.ToString();

        //色設定
        if(viewModel.IsWeakPoint)
        {
            _damageText.color = _weakColor;
        }
        else
        {
            _damageText.color = _normalColor;
        }
        
        //表示切替
        _weakPointObj.SetActive(viewModel.IsWeakPoint);
        _criticalObj.SetActive(viewModel.IsCritical);

        // ワールド座標をスクリーン座標へ変換
        var screenPos = _mainCamera.WorldToScreenPoint(viewModel.WorldPosition);
        _rectTransform.position = screenPos;

        PlayAnimation();
    }

    /// <summary>
    /// ダメージ表記のアニメーション
    /// </summary>
    private void PlayAnimation()
    {
        Debug.Log("ShowDamage開始");
        Sequence seq = DOTween.Sequence();

        //一定時間停止
        float wait = Mathf.Max(0f,_lifeTime - _fadeDuration);
        seq.AppendInterval(wait);

        seq.Append(_rectTransform.DOMoveY(_rectTransform.position.y + _popupDistance,_fadeDuration));
        seq.Join(_canvasGroup.DOFade(0f, _fadeDuration));

        seq.OnComplete(() =>
        {
            Debug.Log("アニメーション完了");
            OnRelease?.Invoke(this);
        });
        _currentTween = seq;
    }
}
