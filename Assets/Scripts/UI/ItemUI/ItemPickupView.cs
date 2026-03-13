using UnityEngine;

/// <summary>
/// アイテムの取得時のUI表示
/// </summary>
public class ItemPickupView : MonoBehaviour, IItemPickupView
{
    [Header("UI")]
    [SerializeField] private GameObject _nearUI;     // 白い丸ポチ
    [SerializeField] private GameObject _interactUI; // 取得キーUI

    [Header("表示高さ")]
    [SerializeField] private Vector3 _displayHeight = new Vector3(0, 1.5f, 0);

    private Camera _mainCamera;
    private Transform _target; //アイテムの位置
    private RectTransform _rectTransform;

    /// <summary>
    /// アイテム位置をpresenterから受け取る
    /// </summary>
    /// <param name="target"></param>
    public void Initialize(Transform target)
    {
        _target = target;   
    }

    public void Hide()
    {
        _nearUI.SetActive(false);
        _interactUI.SetActive(false);
        gameObject.SetActive(false);
    }

    public void ShowNear()
    {
        gameObject.SetActive(true);

        _nearUI.SetActive(true);
        _interactUI.SetActive(false);
    }


    public void ShowInteract()
    {
        gameObject.SetActive(true);

        _nearUI.SetActive(false);
        _interactUI.SetActive(true);
    }

    private void Awake()
    {
        _mainCamera = Camera.main;
        _rectTransform = GetComponent<RectTransform>();
        Hide();
    }

    private void LateUpdate()
    {
        if (_target == null) return;
        if (_mainCamera == null) return;

        Vector3 worldPos = _target.position + _displayHeight;
        Vector3 screenPos = _mainCamera.WorldToScreenPoint(worldPos);

        if (screenPos.z < 0f)
        {
            Hide();
            return;
        }

        _rectTransform.position = screenPos;
    }
}
