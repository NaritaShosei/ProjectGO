using System;

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
        // TODO: Timelineムービーを再生する
        // context.MoviePlayer?.Play(MovieType.Intro, () => context.IsMovieCompleted = true);

        // 仮実装：即座に完了フラグを立てる
        context.IsMovieCompleted = true;
    }

    public SequenceStateType? Tick(SequenceStateContext context, float deltaTime)
    {
        if (context.IsMovieCompleted)
            return SequenceStateType.MobAndSkill;

        return null;
    }

    public void OnExit(SequenceStateContext context)
    {
        // TODO: ムービーを停止する（スキップ時）
    }
}
