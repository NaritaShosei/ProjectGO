using TMPro;
using UnityEngine;

public class SequenceStatusView : MonoBehaviour, ISequenceStatusView
{
    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void SetSequenceName(string sequenceName)
    {
        if (_statusText == null)
            return;

        _statusText.text = sequenceName;
    }

    public void SetProgress(int current)
    {
        if (_statusText == null)
            return;

        _statusText.text = $"{_progressLabel}{current}";
    }

    public void ClearText()
    {
        if (_statusText != null)
            _statusText.text = string.Empty;
    }

    [Header("Text References")]
    [SerializeField] private TextMeshProUGUI _statusText;

    [Header("Display")]
    [SerializeField] private string _progressLabel = "Wave ";
}
