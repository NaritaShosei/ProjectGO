using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResultPanelView : MonoBehaviour
{
    public void SetBossClearTime(string value) => _clearTimeValue.text = value;
    public void SetScore(string value) => _scoreValue.text = value;
    public void SetLevel(string value) => _levelValue.text = value;

    public void Show()
    {
        _root.SetActive(true);
        Canvas.ForceUpdateCanvases();
    }

    public static ResultPanelView CreateRuntime()
    {
        var canvasObject = new GameObject("ResultCanvas", typeof(RectTransform));
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        var view = canvasObject.AddComponent<ResultPanelView>();
        view.BuildHierarchy(canvasObject.transform);
        return view;
    }

    [SerializeField] private GameObject _root;
    [SerializeField] private TextMeshProUGUI _clearTimeValue;
    [SerializeField] private TextMeshProUGUI _scoreValue;
    [SerializeField] private TextMeshProUGUI _levelValue;

    private void BuildHierarchy(Transform parent)
    {
        _root = CreatePanel("Backdrop", parent, new Color(0.015f, 0.02f, 0.03f, 0.92f));
        Stretch(_root.GetComponent<RectTransform>());

        var content = CreatePanel("ResultPanel", _root.transform, new Color(0.055f, 0.07f, 0.09f, 0.98f));
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.sizeDelta = new Vector2(920f, 680f);

        var layout = content.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(80, 80, 56, 56);
        layout.spacing = 20f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        CreateText("Title", content.transform, "BOSS DEFEATED", 56f, FontStyles.Bold, 90f, new Color(0.95f, 0.78f, 0.24f));
        CreateText("ClearTimeLabel", content.transform, "BOSS CLEAR TIME", 24f, FontStyles.Normal, 40f, new Color(0.68f, 0.72f, 0.78f));
        _clearTimeValue = CreateText("ClearTimeValue", content.transform, "00:00.00", 64f, FontStyles.Bold, 90f, Color.white);
        CreateText("ScoreLabel", content.transform, "SCORE", 24f, FontStyles.Normal, 40f, new Color(0.68f, 0.72f, 0.78f));
        _scoreValue = CreateText("ScoreValue", content.transform, "0", 72f, FontStyles.Bold, 100f, new Color(0.95f, 0.78f, 0.24f));
        _levelValue = CreateText("LevelValue", content.transform, "Lv. 1", 36f, FontStyles.Bold, 60f, Color.white);
    }

    private static GameObject CreatePanel(string name, Transform parent, Color color)
    {
        var panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(parent, false);
        panel.GetComponent<Image>().color = color;
        return panel;
    }

    private static TextMeshProUGUI CreateText(
        string name,
        Transform parent,
        string text,
        float fontSize,
        FontStyles style,
        float preferredHeight,
        Color color)
    {
        var textObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI),
            typeof(LayoutElement));
        textObject.transform.SetParent(parent, false);

        var label = textObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;

        textObject.GetComponent<LayoutElement>().preferredHeight = preferredHeight;
        return label;
    }

    private static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
