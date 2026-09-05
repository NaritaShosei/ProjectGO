using TMPro;
using UnityEngine;

public class TextCountDownTimerView : MonoBehaviour, IPhaseTimerView
{
    public void UpdateTimer(float current, float max)
    {
        if (_timerText != null)
        {
            int min = Mathf.FloorToInt(current / 60f);
            int sec = Mathf.FloorToInt(current % 60f);
            int centSec = Mathf.FloorToInt(current * 100f) % 100;

            _timerText.text = $"{min:D2}:{sec:D2}:{centSec:D2}";
        }
    }
    [Header("テキストの参照")]
    [SerializeField] private TextMeshProUGUI _timerText;
}
