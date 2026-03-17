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
