public class EnemyGaugePresenter
{
        public EnemyGaugeView View { get; }

    public EnemyGaugePresenter(IEnemy enemy, EnemyGaugeView view)
    {
        _enemy = enemy;
        View = view;

        View.Initialize(enemy.GetTargetCenter());

        _enemy.OnHealthChanged += HandleHealthChanged;
    }

    public void ResetView()
    {
        View.ResetView();
    }

    public void Dispose()
    {
        _enemy.OnHealthChanged -= HandleHealthChanged;
    }

    private IEnemy _enemy;

    private void HandleHealthChanged(float current, float max)
    {
        View.UpdateGauge(current, max);
    }
}
