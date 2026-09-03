using System;
using UnityEngine;

public class SequenceStatusPresenter : IDisposable
{
    public SequenceStatusPresenter(ISequenceStatusView view)
    {
        _view = view;
    }

    public void Show()
    {
        _view.Show();
    }

    public void Hide()
    {
        _view.Hide();
    }

    public void SetSequenceName(string sequenceName)
    {
        _view.SetSequenceName(sequenceName);
    }

    public void UpdateProgress(int current)
    {
        int safeCurrent = Mathf.Max(0, current);
        _view.SetProgress(safeCurrent);
    }

    public void ClearText()
    {
        _view.ClearText();
    }

    public void Dispose()
    {
        Hide();
    }

    private readonly ISequenceStatusView _view;
}
