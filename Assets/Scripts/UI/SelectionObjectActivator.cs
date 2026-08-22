using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 指定したオブジェクトをオンオフするやつ
/// </summary>
public class SelectionObjectActivator :
    MonoBehaviour,
    ISelectHandler,
    IDeselectHandler
{
    [SerializeField]
    private GameObject _targetObject;
    [SerializeField]
    private Vector2 _upSize = new Vector2(1.5f, 1.5f);

    private Vector2 _originalSize;

    private void Awake()
    {
        if (_targetObject != null)
        {
            _targetObject.SetActive(false);
        }

        _originalSize = transform.localScale;
        _upSize += _originalSize;
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (_targetObject != null)
        {
            _targetObject.SetActive(true);
        }
        transform.localScale = _upSize;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (_targetObject != null)
        {
            _targetObject.SetActive(false);
        }
        transform.localScale = _originalSize;
    }

    private void OnDisable()
    {
        if (_targetObject != null)
        {
            _targetObject.SetActive(false);
        }
    }
}
