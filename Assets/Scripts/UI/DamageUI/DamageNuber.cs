using UnityEngine;
using DG.Tweening;
using TMPro;

public class DamageNuber : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private GameObject _criticalBubble;

    [Header("表示設定")]
    [SerializeField] private float _lifeTime = 1.5f;//消滅までの時間
    [SerializeField] private float _fadeDuration = 0.2f;//フェードの時間
    [SerializeField] private float _popupDistance = 10f;

    private Camera _mainCamera;
    private Vector3 _worldPosition;//敵の座標を収納

    private void Awake()
    {
        _mainCamera = Camera.main;
    }

    public void Initialize(float value, Vector3 hitPoint, DamageType type)
    {
        _worldPosition = hitPoint;//ヒット位置

        _text.text = Mathf.RoundToInt(value).ToString();

        ApplyStyle(type);

        PlayAnimation();
    }

    private void Update()
    {
        transform.position = _mainCamera.WorldToScreenPoint(_worldPosition);
    }

    /// <summary>
    /// ダメージごとのテキストの色を変更
    /// </summary>
    /// <param name="type"></param>
    public void ApplyStyle(DamageType type)
    {
        _criticalBubble.SetActive(true);

        switch (type)
        {
            case DamageType.Normal:
                _text.color = Color.white;
                break;

            case DamageType.Weak:
                _text.color = new Color(1f, 0.45f, 0f);
                break;

            case DamageType.Critical:
                _text.color = Color.white;
                break;
        }
    }

    public void PlayAnimation()
    {
        Sequence seq = DOTween.Sequence();

        _canvasGroup.alpha = 1f;

        seq.AppendInterval(_lifeTime - _fadeDuration);

        seq.Append(transform.DOMoveY(transform.position.y + _popupDistance, _fadeDuration));
        seq.Join(_canvasGroup.DOFade(0f, _fadeDuration));

        seq.OnComplete(() =>
        {
            Destroy(gameObject);
        });

    }
}
