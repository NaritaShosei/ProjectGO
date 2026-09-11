using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

[Serializable]
public class ResultState : ISequenceState
{
    public SequenceStateType StateType => SequenceStateType.Result;

    public void OnEnter(SequenceStateContext context)
    {
        _context = context;
        context.InputHandler?.EnableInput(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (_view == null)
        {
            Debug.LogError("[ResultState] ResultPanelView is not assigned.");
            return;
        }

        _model = new ResultPanelModel(
            context.Result,
            _baseScore,
            _timeScorePerSecond,
            _levelScoreMultiplier);
        _presenter = new ResultPanelPresenter(_view, _model);
        _view.TitleRequested += HandleTitleRequested;
        _presenter.Show();
        context.SequenceManager?.Subtitles?.PlayVoiceSubtitle(SoundCueNames.PlayerVoice.Result);

        context.SequenceManager?.NotifyAllSequencesComplete();
    }

    public SequenceStateType? Tick(SequenceStateContext context, float deltaTime) => null;

    public void OnExit(SequenceStateContext context)
    {
        context.SequenceManager?.Subtitles?.HideSubtitle();
        if (_view != null)
            _view.TitleRequested -= HandleTitleRequested;

        _view?.Hide();
        _model = null;
        _presenter = null;
        _context = null;
    }

    [Header("Result UI")]
    [SerializeField] private ResultPanelView _view;

    [Header("Score Settings")]
    [SerializeField, Min(0)] private int _baseScore = 10000;
    [SerializeField, Min(0f)] private float _timeScorePerSecond = 100f;
    [SerializeField, Min(0)] private int _levelScoreMultiplier = 1000;

    private ResultPanelModel _model;
    private ResultPanelPresenter _presenter;
    private SequenceStateContext _context;

    private void HandleTitleRequested()
    {
        if (!ServiceLocator.TryGet(out SceneTransitionManager transitionManager))
        {
            Debug.LogError("[ResultState] SceneTransitionManagerが見つかりません。");
            return;
        }

        // ロード中の連打で遷移を重複要求しない。
        if (transitionManager.IsTransitioning)
            return;

        _context?.SequenceManager?.Subtitles?.HideSubtitle();
        transitionManager.TransitionToTitle().Forget();
    }
}
