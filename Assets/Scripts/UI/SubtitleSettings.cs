using System;
using UnityEngine;

[CreateAssetMenu(fileName = "SubtitleSettings", menuName = "GameData/SubtitleSettings")]
public sealed class SubtitleSettings : ScriptableObject
{
    public SubtitleLine GetLine(string cueName)
    {
        if (_lines == null) return null;
        foreach (SubtitleLine line in _lines)
        {
            if (line != null && line.CueName == cueName) return line;
        }
        return null;
    }

    [SerializeField] private SubtitleLine[] _lines;
}

[Serializable]
public sealed class SubtitleLine
{
    public string CueName => _cueName;
    public string Text => _text;
    public float Delay => Mathf.Max(0f, _delay);
    public float Duration => Mathf.Max(0f, _duration);
    public float FadeIn => Mathf.Max(0f, _fadeIn);
    public float FadeOut => Mathf.Max(0f, _fadeOut);

    [SerializeField, Tooltip("対応するPlayer_Voiceのキュー名。文章変更時はこの名前を維持する")]
    private string _cueName;
    [SerializeField, TextArea(2, 4)] private string _text;
    [SerializeField, Min(0f), Tooltip("トリガー発生から表示開始までの秒数")]
    private float _delay;
    [SerializeField, Min(0f), Tooltip("フェードを除いた表示継続秒数")]
    private float _duration = 4f;
    [SerializeField, Min(0f)] private float _fadeIn = 0.2f;
    [SerializeField, Min(0f)] private float _fadeOut = 0.3f;
}
