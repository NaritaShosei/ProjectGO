using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 導入ムービーのState。
/// ムービー完了またはスキップで MobAndSkill へ遷移する。
/// 映像はMoviePlayer、字幕とボイスは字幕設定の遅延時間で制御する。
/// </summary>
[Serializable]
public class IntroMovieState : ISequenceState
{
    public SequenceStateType StateType => SequenceStateType.IntroMovie;

    public void OnEnter(SequenceStateContext context)
    {
        _context = context;

        context.InputHandler?.EnableInput(false);
        context.Player?.ForceWarriorMode();

        // スキップ長押し判定用に、Player入力とは独立したUIアクションマップを有効化する。
        _skipInput = new PlayerInput();
        _skipInput.UI.Enable();
        _skipHoldTime = 0f;

        var moviePlayer = context.MoviePlayer;

        if (moviePlayer == null)
        {
            Debug.LogWarning("MoviePlayerが見つかりません。IntroMovieStateを正常に再生できません。");
            context.IsMovieCompleted = true; // MoviePlayerがない場合は即座にムービー完了とする
            return;
        }

        moviePlayer.OnMovieFinished += HandleMovieFinished;

        if(!moviePlayer.PlayMovie(_movieName))
        {
            Debug.LogWarning($"ムービー '{_movieName}' の再生に失敗しました。");
            context.IsMovieCompleted = true; // ムービー再生に失敗した場合も即座にムービー完了とする
            return;
        }

        // TimelineのトラックやSignalは使わず、ムービー開始からの秒数で終盤に合わせる。
        context.SequenceManager?.Subtitles?.PlayVoiceSubtitle(SoundCueNames.PlayerVoice.IntroMovie);
    }

    public SequenceStateType? Tick(SequenceStateContext context, float deltaTime)
    {
        if (context.IsMovieCompleted)
            return _nextSequence;

        UpdateSkipHold(context, deltaTime);

        return null;
    }

    public void OnExit(SequenceStateContext context)
    {
        var moviePlayer = context.MoviePlayer;

        if (moviePlayer != null) moviePlayer.OnMovieFinished -= HandleMovieFinished;
        context.SequenceManager?.Subtitles?.HideSubtitle();

        _skipInput?.UI.Disable();
        _skipInput?.Dispose();
        _skipInput = null;
    }

    [Header("Movie Settings")]
    [SerializeField] private string _movieName = "Intro";
    [Header("スキップ設定")]
    [SerializeField, Tooltip("スキップに必要な長押し時間（秒）")] private float _skipHoldDuration = 1.0f;
    [SerializeField, Tooltip("スキップ可能な上限時刻（秒）。2D→3D切り替わりの時刻。ここを過ぎるとスキップできなくなる")]
    private float _skipBoundaryTime = 25.933333f;
    [Header("シークエンス設定")]
    [SerializeField] private SequenceStateType _nextSequence = SequenceStateType.MobAndSkill;

    private SequenceStateContext _context;
    private PlayerInput _skipInput;
    private float _skipHoldTime;

    private void HandleMovieFinished()
    {
        _context.SequenceManager?.Subtitles?.HideSubtitle();
        _context.IsMovieCompleted = true;
    }

    // ボタン長押しでムービーをスキップする
    private void UpdateSkipHold(SequenceStateContext context, float deltaTime)
    {
        if (_skipInput == null) return;

        // 押していない間はホールド時間をリセット
        if (!_skipInput.UI.Submit.IsPressed())
        {
            _skipHoldTime = 0f;
            return;
        }

        _skipHoldTime += deltaTime;

        if (_skipHoldTime < _skipHoldDuration) return;

        // 境界時刻（2D→3D切り替わり）までスキップし、字幕・ボイスのタイマーも同じ秒数だけ進めて同期させる
        float skippedSeconds = context.MoviePlayer?.SkipTo(_skipBoundaryTime) ?? 0f;
        if (skippedSeconds > 0f)
            context.SequenceManager?.Subtitles?.AdvanceTimeline(skippedSeconds);
    }
}
