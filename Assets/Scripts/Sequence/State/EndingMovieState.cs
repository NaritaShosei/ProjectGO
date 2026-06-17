using System;
using UnityEngine;

/// <summary>
/// エンディングムービーのState。完了またはスキップでリザルトへ遷移する。
/// </summary>
[Serializable]
public class EndingMovieState : ISequenceState
{
    public SequenceStateType StateType => SequenceStateType.EndingMovie;

    public void OnEnter(SequenceStateContext context)
    {
        _context = context;

        context.InputHandler?.EnableInput(false);

        var moviePlayer = context.MoviePlayer;

        if (moviePlayer == null)
        {
            Debug.LogWarning("MoviePlayerが見つかりません。EndingMovieStateを正常に再生できません。");
            context.IsMovieCompleted = true; // MoviePlayerがない場合は即座にムービー完了とする
            return;
        }

        moviePlayer.OnMovieFinished += HandleMovieFinished;

        moviePlayer?.PlayMovie(_movieName);

    }

    public SequenceStateType? Tick(SequenceStateContext context, float deltaTime)
    {
        if (context.IsMovieCompleted)
            return SequenceStateType.Result;

        return null;
    }

    public void OnExit(SequenceStateContext context)
    {
        var moviePlayer = context.MoviePlayer;
        moviePlayer.OnMovieFinished -= HandleMovieFinished;
    }

    [Header("Movie Settings")]
    [SerializeField] private string _movieName = "Ending";
    private SequenceStateContext _context;

    private void HandleMovieFinished()
    {
        _context.IsMovieCompleted = true;
    }
}
