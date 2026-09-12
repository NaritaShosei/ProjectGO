using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// ゲームオーバーのState。
/// 演出ムービーを再生し、Stateに入ってから指定秒数後にタイトルへ遷移する。
/// ゲームオーバーUIは表示せず、ムービー終了やボタン入力を待たない。
/// </summary>
[Serializable]
public class GameOverState : ISequenceState
{
    public SequenceStateType StateType => SequenceStateType.GameOver;

    public void OnEnter(SequenceStateContext context)
    {
        _enteredAt = Time.unscaledTime;
        _isCleanedUp = false;
        _isTransitionRequested = false;
        context.InputHandler?.EnableInput(false);

        // 既存シーンに設定済みのUIも表示しない。
        _gameOverView?.Hide();
        if (_gameOverTimerView != null)
            _gameOverTimerView.gameObject.SetActive(false);

        context.SequenceManager?.Subtitles?.PlayVoiceSubtitle(SoundCueNames.PlayerVoice.Death);

        var moviePlayer = context.MoviePlayer;
        if (moviePlayer == null)
        {
            Debug.LogWarning("MoviePlayerが見つかりません。演出なしで指定秒数後にタイトルへ遷移します。");
            return;
        }

        if (!moviePlayer.PlayMovie(_movieName))
            Debug.LogWarning($"ムービー '{_movieName}' の再生に失敗しました。指定秒数後にタイトルへ遷移します。");
    }

    public SequenceStateType? Tick(SequenceStateContext context, float deltaTime)
    {
        // ポーズやスローモーション中も実時間で計測し、遷移要求は一度だけ送る。
        if (_isTransitionRequested || Time.unscaledTime - _enteredAt < Mathf.Max(0f, _gameOverDuration))
            return null;

        if (!TransitionToTitleScene())
            return null;

        _isTransitionRequested = true;
        Cleanup(context);
        return null;
    }

    public void OnExit(SequenceStateContext context)
    {
        Cleanup(context);
    }

    [Header("Movie Settings")]
    [SerializeField, Tooltip("ゲームオーバー演出ムービーの名前")] private string _movieName = "GameOver";

    [Header("ゲームオーバー設定")]
    [SerializeField, Min(0f), Tooltip("ゲームオーバーステートに入ってからタイトルへの遷移を開始するまでの実時間（秒）")]
    private float _gameOverDuration = 10f;
    [SerializeField, Tooltip("指定秒数後に遷移するシーン名")] private string _titleSceneName = "TitleScene";

    // 既存シーン・Prefabの参照を維持し、配置済みの旧UIを非表示にするためだけに使用する。
    [SerializeField, HideInInspector] private TextCountDownTimerView _gameOverTimerView;
    [SerializeField, HideInInspector] private GameOverView _gameOverView;

    private float _enteredAt;
    private bool _isTransitionRequested;
    private bool _isCleanedUp;

    private bool TransitionToTitleScene()
    {
        if (string.IsNullOrWhiteSpace(_titleSceneName))
        {
            Debug.LogError("[GameOverState] 遷移先シーン名が設定されていません。");
            return false;
        }

        if (!ServiceLocator.TryGet(out SceneTransitionManager transitionManager))
        {
            Debug.LogError("[GameOverState] SceneTransitionManagerが見つかりません。");
            return false;
        }

        transitionManager.TransitionToScene(_titleSceneName).Forget();
        return true;
    }

    private void Cleanup(SequenceStateContext context)
    {
        if (_isCleanedUp)
            return;

        _isCleanedUp = true;
        context.SequenceManager?.Subtitles?.HideSubtitle();
        context.GameOverReason = GameOverReason.None;
    }
}
