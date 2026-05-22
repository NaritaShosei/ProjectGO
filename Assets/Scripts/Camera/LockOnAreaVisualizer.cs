using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ロックオンエリアを画面上に可視化するクラス。
/// </summary>
public class LockOnAreaVisualizer : MonoBehaviour
{
    [Header("表示設定")]
    [SerializeField] private bool _showArea = true;
    [SerializeField] private Color _normalColor = new Color(1f, 1f, 1f, 0.15f);
    [SerializeField] private Color _lockedOnColor = new Color(1f, 0.5f, 0f, 0.25f);

    [Header("参照")]
    [SerializeField] private CameraManager _cameraManager;

    private Image _areaImage;
    private Canvas _canvas;

    private void Awake()
    {
        SetupCanvas();
        SetupAreaImage();
    }

    private void Start()
    {
        UpdateAreaSize();
    }

    private void Update()
    {
        if (_areaImage == null) return;

        _areaImage.enabled = _showArea;
        if (!_showArea) return;

        // ロックオン状態で色を変える
        _areaImage.color = _cameraManager.IsLockedOn ? _lockedOnColor : _normalColor;
    }

    /// <summary>
    /// エリアサイズをCameraManagerの設定に合わせて更新します。
    /// </summary>
    public void UpdateAreaSize()
    {
        if (_areaImage == null) return;

        // CameraManagerの_lockOnAreaRadiusに合わせる
        // publicプロパティ経由で取得する想定
        float radius = _cameraManager != null ? _cameraManager.LockOnAreaRadius : 100f;
        _areaImage.rectTransform.sizeDelta = new Vector2(radius * 2f, radius * 2f);
    }

    private void SetupCanvas()
    {
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 10;

        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();
    }

    private void SetupAreaImage()
    {
        GameObject areaObj = new GameObject("LockOnArea");
        areaObj.transform.SetParent(transform, false);

        _areaImage = areaObj.AddComponent<Image>();
        _areaImage.sprite = CreateCircleSprite();
        _areaImage.color = _normalColor;
        _areaImage.type = Image.Type.Simple;

        // 画面中央に配置
        RectTransform rect = _areaImage.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
    }

    /// <summary>
    /// 円形スプライトをコードで生成します。
    /// </summary>
    private Sprite CreateCircleSprite()
    {
        int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size / 2f;
        float radius = size / 2f;
        float borderWidth = 4f; // 枠線の太さ

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));

                if (dist > radius)
                {
                    // 円の外側：透明
                    tex.SetPixel(x, y, Color.clear);
                }
                else if (dist > radius - borderWidth)
                {
                    // 枠線部分：不透明
                    tex.SetPixel(x, y, Color.white);
                }
                else
                {
                    // 内側：半透明
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, 0.3f));
                }
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}