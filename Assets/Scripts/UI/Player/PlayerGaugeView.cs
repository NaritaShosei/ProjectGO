using UnityEngine;

public class PlayerGaugeView : MonoBehaviour
{
    public void HealthChange(float current, float max, float initialMax)
    {
        _healthView.UpdateGauge(current, max, initialMax);
    }

    /// <summary>
    /// 旧 StaminaChange の代わりに雷ゲージを表示する。
    /// GaugeView はそのまま流用できる。
    /// </summary>
    public void ThunderGaugeChange(float current, float max, float initialMax)
    {
        _thunderGaugeView.UpdateGauge(current, max, initialMax);
    }

    [SerializeField] private GaugeView _healthView;

    /// <summary>
    /// Inspector で旧スタミナゲージと同じ GaugeView を割り当てる。
    /// </summary>
    [SerializeField] private GaugeView _thunderGaugeView;
}
