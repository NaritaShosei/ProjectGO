/// <summary>
/// Behaviour間で共有するランタイム状態コンテキスト
/// </summary>
public class EnemyRuntimeContext
{
    /// <summary>プレイヤーとの距離キャッシュ（各Behaviourが更新する）</summary>
    public float DistanceToPlayer;

    /// <summary>
    /// 攻撃クールダウン残り時間（秒）
    /// MeleeAttackBehaviourが攻撃するたびにCooldown値にセットする
    /// MobEnemy / GoblinEnemy の UpdateEnemy() でdeltaTime（TimeScale反映済み）ずつ減算する
    /// 0以下で攻撃可能。初回はすぐ攻撃できるよう0で初期化する
    /// </summary>
    public float AttackCooldownRemaining;

    /// <summary>
    /// 現在選択中の攻撃パターン
    /// MobEnemyがスロット取得時・攻撃終了後にセット/クリアし、各Behaviourが参照する
    /// </summary>
    public EnemyAttackPattern SelectedPattern;

    /// <summary>
    /// 攻撃終了時にMeleeAttackBehaviourがセットする後退リクエスト。
    /// RetreatBehaviourが参照し、消費後はEnabled=falseにする
    /// </summary>
    public RetreatRequest PendingRetreat = RetreatRequest.None;

    /// <summary>
    /// ObjectPoolから再利用する際に状態を初期値に戻す
    /// </summary>
    public void Reset()
    {
        DistanceToPlayer = 0f;
        AttackCooldownRemaining = 0f;
        SelectedPattern = null;
        PendingRetreat = RetreatRequest.None;
    }

    /// <summary>
    /// 攻撃後の後退要求。EnemyAttackPatternのRetreat関連値をコピーして保持する
    /// </summary>
    public struct RetreatRequest
    {
        public bool Enabled;

        /// <summary>後退を開始するまでの残り硬直時間（秒）。MobEnemy.UpdateEnemy()で減算する</summary>
        public float RecoveryRemaining;
        public float RetreatDistance;
        public float RetreatSpeed;

        public static RetreatRequest None => new RetreatRequest { Enabled = false };
    }
}
