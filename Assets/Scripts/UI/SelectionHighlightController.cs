using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 選択中のボタンにハイライト演出を適用する
/// </summary>
public class SelectionHighlightController :
    MonoBehaviour,
    ISelectHandler,
    IDeselectHandler,
    IPointerEnterHandler
{
    /// <summary>
    /// EventSystemでこのボタンが選択された
    /// </summary>
    public void OnSelect(BaseEventData eventData)
    {
        SetHighlight(true);
    }

    /// <summary>
    /// 別のボタンへ選択が移った
    /// </summary>
    public void OnDeselect(BaseEventData eventData)
    {
        SetHighlight(false);
    }

    /// <summary>
    /// マウスが乗ったボタンをEventSystemで選択する
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (EventSystem.current == null)
            return;

        if (EventSystem.current.currentSelectedGameObject == gameObject)
            return;

        EventSystem.current.SetSelectedGameObject(
            gameObject,
            eventData);
    }

    [SerializeField]
    private GameObject _targetObject;

    [Header("選択時の拡大倍率")]
    [SerializeField]
    private Vector2 _highlightScaleMultiplier =
        new Vector2(1.1f, 1.1f);

    private Vector3 _originalScale;
    private Vector3 _highlightScale;

    private void Awake()
    {
        _originalScale = transform.localScale;

        _highlightScale = new Vector3(
            _originalScale.x * _highlightScaleMultiplier.x,
            _originalScale.y * _highlightScaleMultiplier.y,
            _originalScale.z);

        SetHighlight(false);
    }

    private void OnEnable()
    {
        bool isSelected =
            EventSystem.current != null &&
            EventSystem.current.currentSelectedGameObject == gameObject;

        SetHighlight(isSelected);
    }

    private void SetHighlight(bool isHighlighted)
    {
        if (_targetObject != null)
        {
            _targetObject.SetActive(isHighlighted);
        }

        transform.localScale = isHighlighted
            ? _highlightScale
            : _originalScale;
    }

    private void OnDisable()
    {
        SetHighlight(false);
    }
}
