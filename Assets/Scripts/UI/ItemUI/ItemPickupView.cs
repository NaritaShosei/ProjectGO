using UnityEngine;

/// <summary>
/// アイテムの取得時のUI表示
/// </summary>
public class ItemPickupView : MonoBehaviour, IItemPickupView, IPoolable
{
    public ItemPickupViewState CurrentState => _state;

    // ── IPoolable ────────────────────────────────────────────

    /// <summary>プールから取り出された直後。特別な処理は不要（Initialize で設定される）。</summary>
    public void OnGet() { }

    /// <summary>プールへ返却される直前。Hidden 状態にリセットする。</summary>
    public void OnRelease()
    {
        // Forcibly reset without the guard in SetState
        _state = ItemPickupViewState.Interact; // 同値ガードを回避するためダミー値を先にセット
        SetState(ItemPickupViewState.Hidden);
        _target = null;
        _isBehind = false;
        _canvasGroup.alpha = 1f; // 念のためアルファもリセット
    }

    // ── Public API ───────────────────────────────────────────

    public void Initialize(Transform target)
    {
        _target = target;
        // OnRelease と同じリセット処理
        _state = ItemPickupViewState.Interact;
        SetState(ItemPickupViewState.Hidden);
    }

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

    // ── Inspector ────────────────────────────────────────────

    [Header("UI")]
    [SerializeField] private GameObject _nearUI;
    [SerializeField] private GameObject _interactUI;
    [SerializeField] private CanvasGroup _canvasGroup;

    [Header("表示高さ")]
    [SerializeField] private Vector3 _displayHeight = new Vector3(0, 1.5f, 0);

    // ── Private ──────────────────────────────────────────────

    private Camera _mainCamera;
    private Transform _target;
    private RectTransform _rectTransform;
    private bool _isBehind;
    private ItemPickupViewState _state;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();

        if (_nearUI == null || _interactUI == null || _rectTransform == null || _canvasGroup == null)
        {
            Debug.LogError($"{nameof(ItemPickupView)} : 必須参照が設定されていません", this);
            enabled = false;
            return;
        }

        SetState(ItemPickupViewState.Hidden);
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
        if (_target == null) return;
        if (!TryGetCamera()) return;

        Vector3 worldPos = _target.position + _displayHeight;
        Vector3 screenPos = _mainCamera.WorldToScreenPoint(worldPos);

        if (screenPos.z < 0f)
        {
            if (!_isBehind)
            {
                _canvasGroup.alpha = 0f;
                _isBehind = true;
            }
            return;
        }

        if (_isBehind)
        {
            _canvasGroup.alpha = 1f;
            _isBehind = false;
        }

        _rectTransform.position = screenPos;
    }

    private bool TryGetCamera()
    {
        if (_mainCamera == null || !_mainCamera.isActiveAndEnabled)
            _mainCamera = Camera.main;

        return _mainCamera != null;
    }
}
