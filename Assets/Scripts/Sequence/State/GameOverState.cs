using UnityEngine;


/// <summary>
/// ゲームオーバーのState。
/// 10秒のタイマーで自動的にタイトルへ遷移。
/// リスタートが選ばれた場合はモブ戦の最初からやり直す。
/// </summary>
public class GameOverState : ISequenceState
{
    public SequenceStateType StateType => SequenceStateType.GameOver;

    private const float GameOverDuration = 10f;

    public void OnEnter(SequenceStateContext context)
    {
        context.InputHandler?.EnableInput(false);

        // TODO: ゲームオーバーUIを表示する
        // context.GameOverView?.Show(onRestart, onTitle);

        context.GameOverTimer.StartTimer(GameOverDuration);
        context.GameOverTimer.OnTimeEnded += OnGameOverTimeUp;

        _storedContext = context;
    }

    public SequenceStateType? Tick(SequenceStateContext context, float deltaTime)
    {
        if (context.IsRestartRequested)
        {
            RestartGame(context);
            return SequenceStateType.MobAndSkill;
        }

        if (context.IsTitleRequested || context.IsTimeUp)
        {
            context.SequenceManager?.NotifyTitleRequested();
            return null; // タイトル遷移はSequenceManager経由
        }

        return null;
    }

    public void OnExit(SequenceStateContext context)
    {
        context.GameOverTimer.StopTimer();
        context.GameOverTimer.OnTimeEnded -= OnGameOverTimeUp;

        // TODO: ゲームオーバーUIを非表示
    }

    private SequenceStateContext _storedContext;

    private void OnGameOverTimeUp() => _storedContext.IsTimeUp = true;

    private void RestartGame(SequenceStateContext context)
    {
        // スキルをリセット
        // TODO: SkillManagerにResetメソッドを追加する
        // context.SkillManager?.Reset();

        // プレイヤーのHPをリセット
        // TODO: Player側にリスタート用の初期化メソッドを追加する
        // context.Player?.ReInitialize();

        Debug.Log("[GameOverState] リスタート: モブ戦の最初から");
    }
}
