using System;
using UnityEngine;

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

        return null;
    }

    public void OnExit(SequenceStateContext context)
    {
        var moviePlayer = context.MoviePlayer;

        if (moviePlayer != null) moviePlayer.OnMovieFinished -= HandleMovieFinished;
        context.SequenceManager?.Subtitles?.HideSubtitle();
    }

    [Header("Movie Settings")]
    [SerializeField] private string _movieName = "Intro";
    [Header("シークエンス設定")]
    [SerializeField] private SequenceStateType _nextSequence = SequenceStateType.MobAndSkill;

    private SequenceStateContext _context;

    private void HandleMovieFinished()
    {
        _context.SequenceManager?.Subtitles?.HideSubtitle();
        _context.IsMovieCompleted = true;
    }
}
