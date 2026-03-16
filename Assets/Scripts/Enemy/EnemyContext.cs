public class EnemyContext
{
    public float DistanceToPlayer;

    // MeleeAttackBehaviourが攻撃するたびに更新する
    // BarkBehaviourがクールダウン判定に使用する
    public float LastAttackTime;
}
