/// <summary>
/// EnemyのConditionを管理するインターフェース
/// </summary>
public interface IEnemyConditionController
{
    /// <summary>現在アクションをブロック中か</summary>
    public bool BlocksAction { get; }
    /// <summary>毎フレーム呼ぶ</summary>
    public void Tick(float deltaTime);
    /// <summary>Conditionを適用する</summary>
    public void ApplyCondition(IEnemyCondition condition);
    /// <summary>指定したConditionが存在するか</summary>
    public bool HasCondition(ConditionType type);
}

/// <summary>
/// ConditionControllerをもたないEnemy実装（BossCore / EnemyArmer）向けのNull Objectパターン実装。
/// 全操作は無操作となり、呼び出し側のnullチェックを不要にする。
/// </summary>
public sealed class NullEnemyConditionController : IEnemyConditionController
{
    // アクションを常にブロックしない
    public bool BlocksAction => false;

    public void Tick(float deltaTime) { }
    public void ApplyCondition(IEnemyCondition condition) { }
    public bool HasCondition(ConditionType type) => false;
}
