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
    /// <summary>
    /// 指定したConditionが発動中かを返す
    /// </summary>
    /// <remarks>
    /// 感電（Electrified）・ダウン（Down）などの状態でダメージ倍率が変わる仕様のために使用する想定。
    /// ダメージ計算時に呼び出し元（MobEnemy.TakeDamage など）がConditionを確認し、
    /// DamageContextまたはDamageSystemへ状態を伝える形での活用を予定している。
    /// </remarks>
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
