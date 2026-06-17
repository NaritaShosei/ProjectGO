using System;
using UnityEngine;
/// <summary>
/// ボス登場ムービーのState。
/// 完了またはスキップで BossBattle へ遷移する。
/// </summary>
[Serializable]
public class BossIntroMovieState : ISequenceState
{
    public SequenceStateType StateType => SequenceStateType.BossIntroMovie;

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
            return SequenceStateType.BossBattle;

        return null;
    }

    public void OnExit(SequenceStateContext context)
    {
        var moviePlayer = context.MoviePlayer;
        moviePlayer.OnMovieFinished -= HandleMovieFinished;
    }

    [Header("Movie Settings")]
    [SerializeField] private string _movieName = "Boss";
    private SequenceStateContext _context;

    private void HandleMovieFinished()
    {
        _context.IsMovieCompleted = true;
    }
}
