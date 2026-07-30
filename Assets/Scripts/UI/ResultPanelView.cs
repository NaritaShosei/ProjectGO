using TMPro;
using UnityEngine;

public class ResultPanelView : MonoBehaviour
{
    public void SetBossClearTime(string value)
    {
        if (_clearTimeValue != null)
            _clearTimeValue.text = value;
    }

    public void SetScore(string value)
    {
        if (_scoreValue != null)
            _scoreValue.text = value;
    }

    public void SetLevel(string value)
    {
        if (_levelValue != null)
            _levelValue.text = value;
    }

    public void Show()
    {
        _root.SetActive(true);
        Canvas.ForceUpdateCanvases();
    }

    public void Hide()
    {
        if (_root != null)
            _root.SetActive(false);
    }

    [SerializeField] private GameObject _root;
    [SerializeField] private TextMeshProUGUI _clearTimeValue;
    [SerializeField] private TextMeshProUGUI _scoreValue;
    [SerializeField] private TextMeshProUGUI _levelValue;

    private void Awake()
    {
        Hide();
    }
}
