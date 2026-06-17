using System;
using UnityEngine;


/// <summary>
/// ゲームオーバーのState。
/// 10秒のタイマーで自動的にタイトルへ遷移。
/// リスタートが選ばれた場合はモブ戦の最初からやり直す。
/// </summary>
[Serializable]
public class GameOverState : ISequenceState
{
    public SequenceStateType StateType => SequenceStateType.GameOver;

    public void OnEnter(SequenceStateContext context)
    {
        context.InputHandler?.EnableInput(false);

        // TODO: ゲームオーバーUIを表示する
        // context.GameOverView?.Show(onRestart, onTitle);

        _gameOverTimer = new CountDownTimer();

        if (_gameOverTimerView != null)
            _gameOverTimerPresenter = new CountDownTimerPresenter(_gameOverTimer, _gameOverTimerView);

        _gameOverTimer.OnTimeEnded += OnGameOverTimeUp;
        _gameOverTimer.StartTimer(_gameOverDuration);

        _storedContext = context;
    }

    public SequenceStateType? Tick(SequenceStateContext context, float deltaTime)
    {
        if (context.IsRestartRequested)
        {
            context.IsRestartRequested = false;
            context.IsTitleRequested = false;
            context.IsTimeUp = false;

            RestartGame(context);
            return _restartSequence;
        }

        if (context.IsTitleRequested || context.IsTimeUp)
        {
            context.IsTitleRequested = false;
            context.IsTimeUp = false;

            context.SequenceManager?.NotifyTitleRequested();
            return null; // タイトル遷移はSequenceManager経由
        }

        return null;
    }

    public void OnExit(SequenceStateContext context)
    {
        _gameOverTimer.StopTimer();
        _gameOverTimer.OnTimeEnded -= OnGameOverTimeUp;

        _gameOverTimerPresenter?.Dispose();
        _gameOverTimerPresenter = null;

        // TODO: ゲームオーバーUIを非表示
    }

    [SerializeField] private string _stateName = "GameOverState";

    [Header("ゲームオーバー設定")]
    [SerializeField, Tooltip("ゲームオーバーからタイトルへ遷移するまでの時間（秒）")] private float _gameOverDuration = 10f;
    [SerializeField, Tooltip("ゲームオーバーの残り時間を表示するUI")] private CountDownTimerView _gameOverTimerView;

    [Header("シークエンス設定")]
    [SerializeField, Tooltip("リスタート時に遷移するシークエンス")] private SequenceStateType _restartSequence = SequenceStateType.MobAndSkill;

    private SequenceStateContext _storedContext;
    private CountDownTimer _gameOverTimer;
    private CountDownTimerPresenter _gameOverTimerPresenter;

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
