using UnityEngine;

public class PlayerUIManager : MonoBehaviour
{
    [SerializeField] private GaugeView _hpGauge;
    [SerializeField] private GaugeView _staminaGauge;
    [SerializeField] private ModeView _modeView;
    public GaugeView HPGauge => _hpGauge;
    public GaugeView StaminaGauge => _staminaGauge;
    public ModeView ModeView => _modeView;
}
