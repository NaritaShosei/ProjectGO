public class EnemyContext
{
    public float DistanceToPlayer;

    // 攻撃クールダウン残り時間（秒）
    // MeleeAttackBehaviourが攻撃するたびにAttackCooldown値にセットする
    // MobEnemy.UpdateEnemy()でdeltaTime（TimeScale反映済み）ずつ減算する
    // 0以下で攻撃可能。初回はすぐ攻撃できるよう0で初期化する
    public float AttackCooldownRemaining;
}
