/// <summary>
/// ボス戦のState。制限時間2分。
/// ボス撃破でエンディングムービーへ、時間切れでゲームオーバーへ遷移する。
/// </summary>
public class BossBattleState : ISequenceState
{
    private const float BossBattleDuration = 120f;

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

        context.PhaseTimer.StartTimer(BossBattleDuration);
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
        _context = null;
    }

    private SequenceStateContext _context;

    private void HandleBossDefeated() => _context.IsBossDefeated = true;
    private void HandleTimeUp() => _context.IsTimeUp = true;
}
