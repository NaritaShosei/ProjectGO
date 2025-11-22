using UnityEngine;

public class PlayerUIManager : MonoBehaviour
{
    [SerializeField] private GaugeView _hpGauge;
    [SerializeField] private GaugeView _staminaGauge;

    public GaugeView HPGauge => _hpGauge;
    public GaugeView StaminaGauge => _staminaGauge;
}
