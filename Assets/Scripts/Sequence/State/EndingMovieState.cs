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
        context.InputHandler?.EnableInput(false);

        // TODO: Timelineムービーを再生する
        context.IsMovieCompleted = true;
    }

    public SequenceStateType? Tick(SequenceStateContext context, float deltaTime)
    {
        if (context.IsMovieCompleted)
            return SequenceStateType.Result;

        return null;
    }

    public void OnExit(SequenceStateContext context)
    {
        // TODO: ムービー停止
    }
}
