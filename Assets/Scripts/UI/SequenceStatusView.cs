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
        if (_sequenceNameText == null)
            return;

        _sequenceNameText.text = sequenceName;
    }

    public void SetProgress(int current, int max)
    {
        if (_waveText == null)
            return;

        _waveText.text = $"{_progressLabel}{current}/{max}";
    }

    public void ClearProgress()
    {
        if (_waveText == null)
            return;

        _waveText.text = string.Empty;
    }

    [Header("Text References")]
    [SerializeField] private TextMeshProUGUI _sequenceNameText;
    [SerializeField] private TextMeshProUGUI _waveText;

    [Header("Display")]
    [SerializeField] private string _progressLabel = "SpawnGroup ";
}
