
public sealed class EnemyConditionController : IEnemyConditionController
{
    public bool BlocksAction => _active.BlocksAction;

    public EnemyConditionController(IEnemy enemy)
    {
        _enemy = enemy;
    }

    public void Tick(float deltaTime)
    {
        _active.Tick(_enemy, deltaTime);
    }

    public void ApplyCondition(IEnemyCondition condition)
    {
        _active.Enqueue(_enemy, condition);
        _enemy.OnConditionInterrupt();
    }

    public bool HasCondition(ConditionType type)
        => _active.Has(type);

    private readonly IEnemy _enemy;
    private readonly EnemyConditionQueue _active = new();
}
