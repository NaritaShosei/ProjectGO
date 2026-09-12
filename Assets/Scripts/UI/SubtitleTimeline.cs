using System;

/// <summary>実時間を受け取り、待機・フェード・表示終了を判定する。ゲームの時間倍率に依存しない。</summary>
public sealed class SubtitleTimeline
{
    public bool HasStarted => _elapsed >= _delay;
    public bool IsComplete => _elapsed >= _delay + _fadeIn + _duration + _fadeOut;

    public SubtitleTimeline(float delay, float duration, float fadeIn, float fadeOut)
    {
        _delay = Math.Max(0f, delay);
        _duration = Math.Max(0f, duration);
        _fadeIn = Math.Max(0f, fadeIn);
        _fadeOut = Math.Max(0f, fadeOut);
    }

    public void AdvanceTime(float deltaTime)
    {
        _elapsed += Math.Max(0f, deltaTime);
    }

    public float GetAlpha()
    {
        if (!HasStarted || IsComplete) return 0f;
        float visibleTime = _elapsed - _delay;
        if (_fadeIn > 0f && visibleTime < _fadeIn) return visibleTime / _fadeIn;
        visibleTime -= _fadeIn + _duration;
        if (visibleTime <= 0f) return 1f;
        return _fadeOut > 0f ? Math.Max(0f, 1f - visibleTime / _fadeOut) : 0f;
    }

    private readonly float _delay;
    private readonly float _duration;
    private readonly float _fadeIn;
    private readonly float _fadeOut;
    private float _elapsed;
}
