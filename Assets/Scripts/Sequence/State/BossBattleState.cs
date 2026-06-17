using System;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// ボス戦のState。制限時間2分。
/// ボス撃破でエンディングムービーへ、時間切れでゲームオーバーへ遷移する。
/// </summary>
[Serializable]
public class BossBattleState : ISequenceState
{
    #region パブリック

    public SequenceStateType StateType => SequenceStateType.BossBattle;

    public void OnEnter(SequenceStateContext context)
    {
        _context = context;

        _bossBattleTimer = new CountDownTimer();

        if (_bossBattleTimerView != null)
            _bossBattleTimerPresenter = new CountDownTimerPresenter(_bossBattleTimer, _bossBattleTimerView);

        context.InputHandler?.EnableInput(true);

        // ボスをスポーン
        if (_bossSpawnData != null)
        {
            var strategy = _bossSpawnData.CreateStrategy(context.EnemyManager);
            strategy.Spawn();
        }

        context.EnemyManager.OnBossDefeated += HandleBossDefeated;

        _bossBattleTimer.OnTimeEnded += HandleTimeUp;
        _bossBattleTimer.StartTimer(_bossBattleTimeLimit);
    }

    public SequenceStateType? Tick(SequenceStateContext context, float deltaTime)
    {
        if (context.IsBossDefeated)
            return _bossDefeatedSequence;

        if (context.IsTimeUp)
            return _timeUpSequence;

        return null;
    }

    public void OnExit(SequenceStateContext context)
    {
        context.EnemyManager.OnBossDefeated -= HandleBossDefeated;
        _bossBattleTimer.StopTimer();
        _bossBattleTimer.OnTimeEnded -= HandleTimeUp;

        _bossBattleTimerPresenter?.Dispose();
        _bossBattleTimerPresenter = null;

        // 結果の保存（仮実装。実際はスコア計算などを入れる）
        int killCount = 0;
        int level = 0;
        float clearTime = _bossBattleTimeLimit - _bossBattleTimer.CurrentTime;

        context.Result = new SequenceStateContext.ResultData(killCount, level, clearTime);

        _context = null;
    }

    #endregion

    #region シリアライズ

    [SerializeField] private string _stateName = "BossBattleState";

    [Header("ボス戦設定")]
    [SerializeField, Tooltip("ボス戦の時間制限（秒）")] private float _bossBattleTimeLimit = 120f;
    [SerializeField, Tooltip("ボス戦のタイマーUI")] private CountDownTimerView _bossBattleTimerView;
    [SerializeField, Tooltip("ボスのスポーンデータ")] private SpawnData _bossSpawnData;
    [Header("シークエンス")]
    [SerializeField, Tooltip("タイムアップ時に遷移するシークエンス")] private SequenceStateType _timeUpSequence = SequenceStateType.GameOver;
    [SerializeField, Tooltip("ボス撃破時に遷移するシークエンス")] private SequenceStateType _bossDefeatedSequence = SequenceStateType.EndingMovie;

    #endregion

    #region プライベート

    private SequenceStateContext _context;
    private CountDownTimer _bossBattleTimer;
    private CountDownTimerPresenter _bossBattleTimerPresenter;

    #endregion

    #region イベントハンドラー

    private void HandleBossDefeated() => _context.IsBossDefeated = true;
    private void HandleTimeUp() => _context.IsTimeUp = true;

    #endregion
}
