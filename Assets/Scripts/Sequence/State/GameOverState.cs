using System;
using Cysharp.Threading.Tasks;
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

        _isDisplayTimeEnded = false;
        var reason = context.GameOverReason == GameOverReason.None
            ? GameOverReason.PlayerHealthDepleted
            : context.GameOverReason;
        context.IsTimeUp = false;

        if (_gameOverView == null)
        {
            Debug.LogError("[GameOverState] シーンに配置したGameOverViewが設定されていません。");
        }
        else
        {
            _gameOverPresenter = new GameOverPresenter(
                new GameOverModel(reason),
                _gameOverView,
                () => context.IsTitleRequested = true);
            _gameOverPresenter.Show();
        }

        _gameOverTimer = new CountDownTimer();

        if (_gameOverTimerView != null)
            _gameOverTimerPresenter = new CountDownTimerPresenter(_gameOverTimer, _gameOverTimerView);

        _gameOverTimer.OnTimeEnded += OnGameOverTimeUp;
        _gameOverTimer.StartTimer(_gameOverDuration);

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

        if (context.IsTitleRequested || _isDisplayTimeEnded)
        {
            context.IsTitleRequested = false;
            _isDisplayTimeEnded = false;

            TransitionToTitleScene();
            return null;
        }

        return null;
    }

    public void OnExit(SequenceStateContext context)
    {
        _gameOverTimer.StopTimer();
        _gameOverTimer.OnTimeEnded -= OnGameOverTimeUp;

        _gameOverTimerPresenter?.Dispose();
        _gameOverTimerPresenter = null;

        _gameOverPresenter?.Dispose();
        _gameOverPresenter = null;
        context.GameOverReason = GameOverReason.None;
    }

    [Header("ゲームオーバー設定")]
    [SerializeField, Tooltip("ゲームオーバーからタイトルへ遷移するまでの時間（秒）")] private float _gameOverDuration = 10f;
    [SerializeField, Tooltip("ゲームオーバーの残り時間を表示するUI")] private CountDownTimerView _gameOverTimerView;
    [SerializeField, Tooltip("ゲームオーバー表示UI（MVPのView）")] private GameOverView _gameOverView;
    [SerializeField, Tooltip("タイトルへ戻るボタンで遷移するシーン名")] private string _titleSceneName = "GOTestScene";

    [Header("シークエンス設定")]
    [SerializeField, Tooltip("リスタート時に遷移するシークエンス")] private SequenceStateType _restartSequence = SequenceStateType.MobAndSkill;

    private CountDownTimer _gameOverTimer;
    private CountDownTimerPresenter _gameOverTimerPresenter;
    private GameOverPresenter _gameOverPresenter;
    private bool _isDisplayTimeEnded;

    private void OnGameOverTimeUp() => _isDisplayTimeEnded = true;

    private void TransitionToTitleScene()
    {
        if (string.IsNullOrWhiteSpace(_titleSceneName))
        {
            Debug.LogError("[GameOverState] 遷移先シーン名が設定されていません。");
            return;
        }

        if (!ServiceLocator.TryGet(out SceneTransitionManager transitionManager))
        {
            Debug.LogError("[GameOverState] SceneTransitionManagerが見つかりません。");
            return;
        }

        transitionManager.TransitionToScene(_titleSceneName).Forget();
    }

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
