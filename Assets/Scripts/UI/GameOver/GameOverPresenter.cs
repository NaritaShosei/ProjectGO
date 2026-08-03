using System;

public sealed class GameOverPresenter : IDisposable
{
    public GameOverPresenter(GameOverModel model, IGameOverView view, Action onTitleRequested)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _view = view ?? throw new ArgumentNullException(nameof(view));
        _onTitleRequested = onTitleRequested ?? throw new ArgumentNullException(nameof(onTitleRequested));
        _view.TitleRequested += HandleTitleRequested;
    }

    public void Show() => _view.Show(_model.DisplayText, _model.Reason);
    public void Dispose()
    {
        _view.TitleRequested -= HandleTitleRequested;
        _view.Hide();
    }

    private readonly GameOverModel _model;
    private readonly IGameOverView _view;
    private readonly Action _onTitleRequested;

    private void HandleTitleRequested() => _onTitleRequested.Invoke();
}
