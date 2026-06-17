using System;
using UnityEngine;

/// <summary>
/// 導入ムービーのState。
/// ムービー完了またはスキップで MobAndSkill へ遷移する。
/// 実際のTimeline再生は IMoviePlayer 経由で行う（仮実装ではフラグで即完了）。
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

        moviePlayer.OnMovieFinished += HandleMovieFinished;

        moviePlayer?.PlayMovie(_movieName);
    }

    public SequenceStateType? Tick(SequenceStateContext context, float deltaTime)
    {
        if (context.IsMovieCompleted)
            return SequenceStateType.MobAndSkill;

        return null;
    }

    public void OnExit(SequenceStateContext context)
    {
        var moviePlayer = context.MoviePlayer;

        moviePlayer.OnMovieFinished -= HandleMovieFinished;
    }

    [Header("Movie Settings")]
    [SerializeField] private string _movieName = "Intro";
    private SequenceStateContext _context;

    private void HandleMovieFinished()
    {
        _context.IsMovieCompleted = true;
    }
}
