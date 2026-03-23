/// <summary>
/// MobEnemyが保持するConditionControllerの実装
/// ConditionQueueへの委譲とEnemy本体への割り込み通知を担う
/// </summary>
public sealed class EnemyConditionController : IEnemyConditionController
{
    public bool BlocksAction => _active.BlocksAction;

    public EnemyConditionController(IEnemy enemy)
    {
        _enemy = enemy;
    }

    /// <summary>
    /// 毎フレームConditionQueueを進める
    /// </summary>
    public void Tick(float deltaTime)
    {
        _active.Tick(_enemy, deltaTime);
    }

    /// <summary>
    /// Conditionをキューに追加し、Enemyの現在行動を割り込み終了させる
    /// </summary>
    public void ApplyCondition(IEnemyCondition condition)
    {
        _active.Enqueue(_enemy, condition);
        _enemy.OnConditionInterrupt();
    }

    /// <summary>
    /// 指定したConditionが発動中かを返す
    /// </summary>
    public bool HasCondition(ConditionType type)
        => _active.Has(type);

    private readonly IEnemy _enemy;
    private readonly EnemyConditionQueue _active = new();
}
