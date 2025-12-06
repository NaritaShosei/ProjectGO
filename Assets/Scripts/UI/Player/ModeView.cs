using UnityEngine;
using UnityEngine.UI;

public class ModeView : MonoBehaviour
{
    [SerializeField] private Image _image;
    [SerializeField] private Color _thunderColor = Color.yellow;
    [SerializeField] private Color _battleColor = Color.orange;

    public void UpdateView(PlayerModeType type)
    {
        var color = type switch
        {
            PlayerModeType.Thunder => _thunderColor,
            PlayerModeType.Battle => _battleColor,
        };

        _image.color = color;
    }
}
