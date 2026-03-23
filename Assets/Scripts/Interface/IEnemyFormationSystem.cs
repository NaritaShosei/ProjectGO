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
    /// 後衛Enemyが被弾したことを通知する
    /// CP同等以下の前衛と入れ替えてスロットを再配分する
    /// </summary>
    void NotifyHit(int enemyId);
}
