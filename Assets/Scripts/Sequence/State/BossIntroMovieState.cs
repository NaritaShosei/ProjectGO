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

        if (moviePlayer == null)
        {
            Debug.LogWarning("MoviePlayerが見つかりません。BossIntroMovieStateを正常に再生できません。");
            context.IsMovieCompleted = true; // MoviePlayerがない場合は即座にムービー完了とする
            return;
        }

        moviePlayer.OnMovieFinished += HandleMovieFinished;

        if (!moviePlayer.PlayMovie(_movieName))
        {
            Debug.LogWarning($"ムービー '{_movieName}' の再生に失敗しました。");
            context.IsMovieCompleted = true; // ムービー再生に失敗した場合も即座にムービー完了とする
        }
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
        moviePlayer.OnMovieFinished -= HandleMovieFinished;
    }

    [SerializeField] private string _stateName = "BossIntroMovieState";

    [Header("Movie Settings")]
    [SerializeField] private string _movieName = "Boss";
    [Header("シークエンス設定")]
    [SerializeField] private SequenceStateType _nextSequence = SequenceStateType.BossBattle;

    private SequenceStateContext _context;

    private void HandleMovieFinished()
    {
        _context.IsMovieCompleted = true;
    }
}
