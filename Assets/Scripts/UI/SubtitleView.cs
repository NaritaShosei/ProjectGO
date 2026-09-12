using TMPro;
using UnityEngine;

public sealed class SubtitleView : MonoBehaviour
{
    public bool InitializeView()
    {
        if (_text == null)
        {
            Debug.LogError("[SubtitleView] 事前配置したText UIを設定してください。", this);
            return false;
        }
        _text.raycastTarget = false;
        _text.richText = false;
        HideSubtitle();
        return true;
    }

    public void SetText(string text)
    {
        if (_text != null) _text.text = text;
    }

    public void SetAlpha(float alpha)
    {
        // 配置済みTextの色やレイアウトを維持し、透明度だけを変更する。
        if (_text != null) _text.alpha = alpha;
    }

    public void HideSubtitle()
    {
        SetAlpha(0f);
        if (_text != null) _text.text = string.Empty;
    }

    [SerializeField, Tooltip("事前配置した字幕用TextMeshPro UI")]
    private TMP_Text _text;

    private void Awake()
    {
        HideSubtitle();
    }
}
