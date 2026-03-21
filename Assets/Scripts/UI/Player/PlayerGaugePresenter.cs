/// <summary>
/// HP と雷ゲージの変化を PlayerGaugeView へ反映する Presenter。
/// </summary>
public class PlayerGaugePresenter
{
    public PlayerGaugePresenter(
        IHealth health,
        IPlayerStats playerStats,
        PlayerGaugeView view)
    {
        _health = health;
        _playerStats = playerStats;
        _view = view;

        Bind();
    }

    public void Dispose()
    {
        _health.OnHealthChanged -= _view.HealthChange;
        _playerStats.OnThunderGaugeChanged -= _view.ThunderGaugeChange;
    }

    private readonly IHealth _health;
    private readonly IPlayerStats _playerStats;
    private readonly PlayerGaugeView _view;

    private void Bind()
    {
        _health.OnHealthChanged += _view.HealthChange;
        _playerStats.OnThunderGaugeChanged += _view.ThunderGaugeChange;

        // 起動時に初期値を即反映
        _view.ThunderGaugeChange(
            _playerStats.CurrentThunderGauge,
            _playerStats.MaxThunderGauge,
            _playerStats.InitialMaxThunderGauge
        );
    }
}
