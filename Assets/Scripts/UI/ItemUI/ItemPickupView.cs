using UnityEngine;

/// <summary>
/// アイテムの取得時のUI表示
/// </summary>
public class ItemPickupView : MonoBehaviour, IItemPickupView
{
    public void SetState(ItemPickupViewState state)
    {
        if (_state == state) return;
        _state = state;

        switch (state)
        {
            case ItemPickupViewState.Hidden:
                gameObject.SetActive(false);
                break;

            case ItemPickupViewState.Near:
                gameObject.SetActive(true);
                _nearUI.SetActive(true);
                _interactUI.SetActive(false);
                break;

            case ItemPickupViewState.Interact:
                gameObject.SetActive(true);
                _nearUI.SetActive(false);
                _interactUI.SetActive(true);
                break;
        }
    }

    /// <summary>
    /// アイテム位置をpresenterから受け取る
    /// </summary>
    /// <param name="target"></param>
    public void Initialize(Transform target)
    {
        _target = target;
    }

    [Header("UI")]
    [SerializeField] private GameObject _nearUI;     // 白い丸ポチ
    [SerializeField] private GameObject _interactUI; // 取得キーUI

    [Header("表示高さ")]
    [SerializeField] private Vector3 _displayHeight = new Vector3(0, 1.5f, 0);

    private Camera _mainCamera;
    private Transform _target; //アイテムの位置
    private RectTransform _rectTransform;

    private ItemPickupViewState _state;

    private void Awake()
    {
        _mainCamera = Camera.main;
        _rectTransform = GetComponent<RectTransform>();
        SetState(ItemPickupViewState.Hidden);
        gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (_target == null) return;
        if (!TryGetCamera()) return;

        Vector3 worldPos = _target.position + _displayHeight;
        Vector3 screenPos = _mainCamera.WorldToScreenPoint(worldPos);

        if (screenPos.z < 0f)
        {
            return;
        }

        _rectTransform.position = screenPos;
    }

    private bool TryGetCamera()
    {
        if (_mainCamera == null || !_mainCamera.isActiveAndEnabled)
        {
            _mainCamera = Camera.main;
        }

        return _mainCamera != null;
    }
}
