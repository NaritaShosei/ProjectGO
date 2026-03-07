using UnityEngine;

public class PlayerGaugeView : MonoBehaviour
{
    public void HealthChange(float current, float max, float initialMax)
    {
        _healthView.UpdateGauge(current, max, initialMax);
    }

    public void StaminaChange(float current, float max, float initialMax)
    {
        _staminaView.UpdateGauge(current, max, initialMax);
    }

    [SerializeField] private GaugeView _healthView;
    [SerializeField] private GaugeView _staminaView;
}
