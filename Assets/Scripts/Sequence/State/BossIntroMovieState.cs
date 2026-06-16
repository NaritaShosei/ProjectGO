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
        context.InputHandler?.EnableInput(false);

        // TODO: Timelineムービーを再生する
        // context.MoviePlayer?.Play(MovieType.BossIntro, () => context.IsMovieCompleted = true);

        context.IsMovieCompleted = true;
    }

    public SequenceStateType? Tick(SequenceStateContext context, float deltaTime)
    {
        if (context.IsMovieCompleted)
            return SequenceStateType.BossBattle;

        return null;
    }

    public void OnExit(SequenceStateContext context)
    {
        // TODO: ムービー停止
    }
}
