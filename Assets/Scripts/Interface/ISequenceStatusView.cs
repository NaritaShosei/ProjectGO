public interface ISequenceStatusView
{
    void Show();
    void Hide();
    void SetSequenceName(string sequenceName);
    void SetProgress(int current, int max);
    void ClearProgress();
}
