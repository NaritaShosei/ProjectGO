using UnityEngine;

public sealed class ResultPanelPresenter
{
    public ResultPanelPresenter(ResultPanelView view, ResultPanelModel model)
    {
        _view = view;
        _model = model;
    }

    public void Show()
    {
        int totalCentiseconds = Mathf.RoundToInt(_model.BossClearTime * 100f);
        int minutes = totalCentiseconds / 6000;
        int seconds = totalCentiseconds / 100 % 60;
        int centiseconds = totalCentiseconds % 100;

        _view.SetBossClearTime($"{minutes:00}:{seconds:00}.{centiseconds:00}");
        _view.SetScore(_model.Score.ToString("N0"));
        _view.SetLevel($"Lv. {_model.Level}");
        _view.Show();
    }

    private readonly ResultPanelView _view;
    private readonly ResultPanelModel _model;
}
