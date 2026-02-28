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

    public event Action<IDamagePopupView> OnRelease;

    private Tween _currentTween;
    private Camera _mainCamera;
    private void Awake()
    {
        _mainCamera = Camera.main;
    }

    public void ShowDamage(DamagePopupViewModel viewModel)
    {
        _damageText.text = viewModel.Damage.ToString();

        _weakPointObj.SetActive(viewModel.IsWeakPoint);
        _criticalObj.SetActive(viewModel.IsCritical);

        // ワールド座標 → スクリーン座標へ変換
        var screenPos = _mainCamera.WorldToScreenPoint(viewModel.WorldPosition);
        transform.position = screenPos;

        PlayAnimation();
    }

    private void PlayAnimation()
    {
        Sequence seq = DOTween.Sequence();


        seq.AppendInterval(_lifeTime - _fadeDuration);

        seq.Append(transform.DOMoveY(transform.position.y + _popupDistance, _fadeDuration));
        seq.Join(_canvasGroup.DOFade(0f, _fadeDuration));

        seq.OnComplete(() =>
        {
            OnRelease?.Invoke(this);
        });
        _currentTween = seq;
    }
}
