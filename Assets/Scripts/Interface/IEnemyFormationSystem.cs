/// <summary>
/// フォーメーション管理システムのインターフェース
/// IEnemyAttackerSlotを拡張して前衛・後衛の管理機能を追加する
/// 既存のBehaviourはIEnemyAttackerSlotのみを参照するため、変更不要
/// </summary>
public interface IEnemyFormationSystem : IEnemyAttackerSlot
{
    /// <summary>
    /// EnemyをFormationSystemに登録する
    /// EnemyManager.Spawn内でenemy.Init()より前に呼ぶこと
    /// （Init内のTryAcquireが正しく評価されるため）
    /// </summary>
    void Register(IEnemy enemy, IFormationParticipant participant);

    /// <summary>
    /// 毎フレーム呼び出す
    /// 背後に移動かつCoolDown中の前衛からスロットを正面の低CPエントリへ譲渡する
    /// </summary>
    void Tick(float deltaTime);
}
