using UnityEngine;

public class PlayerGaugeView : MonoBehaviour
{
    public void HealthChange(float current, float max)
    {
        _healthView.UpdateGauge(current, max);
    }

    public void StaminaChange(float current, float max)
    {
        _staminaView.UpdateGauge(current, max);
    }

    [SerializeField] private GaugeView _healthView;
    [SerializeField] private GaugeView _staminaView;
}
