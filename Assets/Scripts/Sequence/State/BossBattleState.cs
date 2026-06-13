using System;

/// <summary>
/// ボス戦のState。制限時間2分。
/// ボス撃破でエンディングムービーへ、時間切れでゲームオーバーへ遷移する。
/// </summary>
public class BossBattleState : ISequenceState
{
    public SequenceStateType StateType => SequenceStateType.BossBattle;

    public void OnEnter(SequenceStateContext context)
    {
        _context = context;

        context.InputHandler?.EnableInput(true);

        // ボスをスポーン
        if (context.BossSpawnData != null)
        {
            var strategy = context.BossSpawnData.CreateStrategy(context.EnemyManager);
            strategy.Spawn();
        }

        context.EnemyManager.OnBossDefeated += HandleBossDefeated;

        context.PhaseTimer.StartTimer(context.BossBattleTimeLimit);
        context.PhaseTimer.OnTimeEnded += HandleTimeUp;
    }

    public SequenceStateType? Tick(SequenceStateContext context, float deltaTime)
    {
        if (context.IsBossDefeated)
            return SequenceStateType.EndingMovie;

        if (context.IsTimeUp)
            return SequenceStateType.GameOver;

        return null;
    }

    public void OnExit(SequenceStateContext context)
    {
        context.EnemyManager.OnBossDefeated -= HandleBossDefeated;
        context.PhaseTimer.StopTimer();
        context.PhaseTimer.OnTimeEnded -= HandleTimeUp;

        // 結果の保存（仮実装。実際はスコア計算などを入れる）
        int killCount = 0;
        int level = 0;
        float clearTime = context.BossBattleTimeLimit - context.PhaseTimer.CurrentTime;

        context.Result = new SequenceStateContext.ResultData(killCount, level, clearTime);

        _context = null;
    }

    private SequenceStateContext _context;

    private void HandleBossDefeated() => _context.IsBossDefeated = true;
    private void HandleTimeUp() => _context.IsTimeUp = true;
}
