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
    /// Conditionを pending を経由せず即座に登録し OnEnter() を呼ぶ。
    /// _isDead = true 後など Tick() が止まった状態での適用に使用する。
    /// </summary>
    public void ApplyImmediate(IEnemyCondition condition)
    {
        _active.Apply(_enemy, condition);
    }

    /// <summary>
    /// 指定したConditionが発動中かを返す
    /// </summary>
    public bool HasCondition(ConditionType type)
        => _active.Has(type);

    /// <summary>
    /// ObjectPoolから再利用する際にすべてのConditionを強制終了してクリアする
    /// </summary>
    public void Clear()
    {
        _active.Clear(_enemy);
    }

    private readonly IEnemy _enemy;
    private readonly EnemyConditionQueue _active = new();
}
