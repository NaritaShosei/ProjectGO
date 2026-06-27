using System;
using UnityEngine;

public class SequenceStatusPresenter : IDisposable
{
    public SequenceStatusPresenter(ISequenceStatusView view, string sequenceName)
    {
        _view = view;
        _sequenceName = sequenceName;
    }

    public void Show()
    {
        _view.Show();
        _view.SetSequenceName(_sequenceName);
    }

    public void Hide()
    {
        _view.Hide();
    }

    public void UpdateProgress(int current, int max)
    {
        int safeCurrent = Mathf.Max(0, current);
        int safeMax = Mathf.Max(0, max);
        _view.SetProgress(safeCurrent, safeMax);
    }

    public void ClearProgress()
    {
        _view.ClearProgress();
    }

    public void Dispose()
    {
        Hide();
    }

    private readonly ISequenceStatusView _view;
    private readonly string _sequenceName;
}
