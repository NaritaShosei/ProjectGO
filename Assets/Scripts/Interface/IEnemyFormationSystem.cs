using System.Collections.Generic;

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
    /// EnemyをFormationSystemから登録解除する。
    /// プールへの強制返却時にも呼び出す。
    /// </summary>
    void Unregister(IEnemy enemy);

    /// <summary>
    /// 後衛Enemyが被弾したことを通知する
    /// CP同等以下の前衛と入れ替えてスロットを再配分する
    /// </summary>
    void NotifyHit(int enemyId);

    /// <summary>
    /// グループを待機状態として登録する。
    /// 待機中のメンバーは通常の前衛選出から除外される。
    /// </summary>
    void RegisterWaitingGroup(EnemyGroup group);

    /// <summary>
    /// グループが前衛へ出られるか判定する。
    /// </summary>
    bool CanPromoteGroup(EnemyGroup group);

    /// <summary>
    /// 条件を満たしていればグループ全員を前衛へ昇格させる。
    /// </summary>
    bool TryPromoteGroup(EnemyGroup group);
}
