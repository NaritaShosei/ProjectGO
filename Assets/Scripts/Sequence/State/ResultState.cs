using System;
using UnityEngine;

[Serializable]
public class ResultState : ISequenceState
{
    public SequenceStateType StateType => SequenceStateType.Result;

    public void OnEnter(SequenceStateContext context)
    {
        context.InputHandler?.EnableInput(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        _viewInstance = ResultUIManager.GetOrCreateView(_view);
        _model = new ResultPanelModel(
            context.Result,
            _baseScore,
            _timeScorePerSecond,
            _levelScoreMultiplier);
        _presenter = new ResultPanelPresenter(_viewInstance, _model);
        _presenter.Show();

        context.SequenceManager?.NotifyAllSequencesComplete();
    }

    public SequenceStateType? Tick(SequenceStateContext context, float deltaTime) => null;

    public void OnExit(SequenceStateContext context)
    {
        if (_view == null && _viewInstance != null)
            UnityEngine.Object.Destroy(_viewInstance.gameObject);

        _viewInstance = null;
        _model = null;
        _presenter = null;
    }

    [Header("Result UI")]
    [SerializeField] private ResultPanelView _view;

    [Header("Score Settings")]
    [SerializeField, Min(0)] private int _baseScore = 10000;
    [SerializeField, Min(0f)] private float _timeScorePerSecond = 100f;
    [SerializeField, Min(0)] private int _levelScoreMultiplier = 1000;

    private ResultPanelView _viewInstance;
    private ResultPanelModel _model;
    private ResultPanelPresenter _presenter;
}
