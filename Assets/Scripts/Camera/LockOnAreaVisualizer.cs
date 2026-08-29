using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ロックオンエリアを画面上に可視化するクラス。
/// エディタ再生前でも表示され、値変更時に即座に反映されます。
/// </summary>
public class LockOnAreaVisualizer : MonoBehaviour
{
    /// <summary>
    /// CameraManagerのロックオンエリア半径をUIの直径へ反映します。
    /// </summary>
    public void UpdateAreaSize()
    {
        if (_areaImage == null || _cameraManager == null) return;

        float radius = _cameraManager.LockOnAreaRadius;
        _areaImage.rectTransform.sizeDelta = new Vector2(radius * 2f, radius * 2f);
    }

    [Header("表示設定")]
    [SerializeField] private bool _showArea = true;
    [SerializeField] private Color _normalColor = new Color(1f, 1f, 1f, 0.15f);
    [SerializeField] private Color _lockedOnColor = new Color(1f, 0.5f, 0f, 0.25f);

    [Header("参照")]
    [SerializeField] private CameraManager _cameraManager;

    private Image _areaImage;
    private Canvas _canvas;
    private Sprite _circleSprite;
    private float _lastRadius = -1f;

    private void OnEnable()
    {
        if (_areaImage == null)
        {
            Initialize();
        }
        UpdateAreaSize();
    }

    private void Awake()
    {
        Initialize();
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

        _areaImage.color = _cameraManager.IsLockedOn ? _lockedOnColor : _normalColor;

        // 値が変わった時だけ更新
        float currentRadius = _cameraManager.LockOnAreaRadius;
        if (!Mathf.Approximately(_lastRadius, currentRadius))
        {
            UpdateAreaSize();
            _lastRadius = currentRadius;

#if UNITY_EDITOR
            Debug.Log($"[LockOnAreaVisualizer] LockOnAreaRadius が変更されました: {_lastRadius}px");
#endif
        }
    }

    /// <summary>
    /// インスペクタで値が変更された時に呼ばれます（エディタのみ）。
    /// </summary>
    private void OnValidate()
    {
#if UNITY_EDITOR
        if (Application.isPlaying) return;
        if (_areaImage != null)
        {
            UpdateAreaSize();
        }
#endif
    }

    private void OnDestroy()
    {
        if (_circleSprite != null && _circleSprite.texture != null)
        {
            DestroyImmediate(_circleSprite.texture);
            _circleSprite = null;
        }
    }

    /// <summary>
    /// 初期化処理。Canvas・Image・Spriteをまとめてセットアップします。
    /// </summary>
    private void Initialize()
    {
        if (_cameraManager == null)
        {
            Debug.LogWarning("[LockOnAreaVisualizer] CameraManagerの参照がありません。", gameObject);
            enabled = false;
            return;
        }

        // Canvasのセットアップ（既存があれば使い回す）
        _canvas = GetComponent<Canvas>();
        if (_canvas == null)
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 10;
            gameObject.AddComponent<CanvasScaler>();
            gameObject.AddComponent<GraphicRaycaster>();
        }

        // Imageのセットアップ（既存があれば使い回す）
        _areaImage = GetComponentInChildren<Image>();
        if (_areaImage == null)
        {
            SetupAreaImage();
        }

        // Spriteのセットアップ（既存があれば使い回す）
        if (_circleSprite == null)
        {
            _circleSprite = CreateCircleSprite();
        }

        _areaImage.sprite = _circleSprite;
        _lastRadius = -1f; // 強制更新フラグ
    }

    private void SetupAreaImage()
    {
        GameObject areaObj = new GameObject("LockOnArea");
        areaObj.transform.SetParent(transform, false);

        _areaImage = areaObj.AddComponent<Image>();
        _areaImage.color = _normalColor;
        _areaImage.type = Image.Type.Simple;

        RectTransform rect = _areaImage.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
    }

    /// <summary>
    /// 円形スプライトをコードで生成します。
    /// 一度だけ生成してキャッシュします。
    /// </summary>
    private Sprite CreateCircleSprite()
    {
        int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.name = "LockOnAreaCircle";
        tex.filterMode = FilterMode.Bilinear;

        float center = size / 2f;
        float radius = size / 2f;
        float borderWidth = 4f;

        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                int i = y * size + x;

                if (dist > radius)
                {
                    pixels[i] = Color.clear;
                }
                else if (dist > radius - borderWidth)
                {
                    pixels[i] = Color.white;
                }
                else
                {
                    pixels[i] = new Color(1f, 1f, 1f, 0.3f);
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}