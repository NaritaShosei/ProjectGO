using System;

public interface IGameOverView
{
    event Action TitleRequested;

    void Show(string message, GameOverReason reason);
    void Hide();
}
