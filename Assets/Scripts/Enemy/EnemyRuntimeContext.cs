/// <summary>
/// Behaviour間で共有するランタイム状態コンテキスト
/// </summary>
public class EnemyRuntimeContext
{
    /// <summary>プレイヤーとの距離キャッシュ（各Behaviourが更新する）</summary>
    public float DistanceToPlayer;

    // 攻撃クールダウン残り時間（秒）
    // MeleeAttackBehaviourが攻撃するたびにCooldown値にセットする
    // MobEnemy.UpdateEnemy()でdeltaTime（TimeScale反映済み）ずつ減算する
    // 0以下で攻撃可能。初回はすぐ攻撃できるよう0で初期化する
    public float AttackCooldownRemaining;

    /// <summary>
    /// 現在選択中の攻撃パターン
    /// MobEnemyがスロット取得時・攻撃終了後にセット/クリアし、各Behaviourが参照する
    /// </summary>
    public EnemyAttackPattern SelectedPattern;

    /// <summary>
    /// スロット取得時のコスト。解放時に一致させる必要があるため保持する
    /// </summary>
    public int AcquiredSlotCost = 1;
}
