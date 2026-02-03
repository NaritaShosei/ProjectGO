public class PlayerGaugePresenter
{
    public PlayerGaugePresenter(
    IHealth health,
    IStamina stamina,
    PlayerGaugeView view)
    {
        _health = health;
        _stamina = stamina;
        _view = view;

        Bind();
    }

    public void Dispose()
    {
        _health.OnHealthChanged -= _view.HealthChange;
        _stamina.OnStaminaChanged -= _view.StaminaChange;
    }

    private readonly IHealth _health;
    private readonly IStamina _stamina;
    private readonly PlayerGaugeView _view;

    private void Bind()
    {
        _health.OnHealthChanged += _view.HealthChange;
        _stamina.OnStaminaChanged += _view.StaminaChange;
    }
}
