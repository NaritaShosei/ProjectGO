public class EnemyContext
{
    public float DistanceToPlayer;

    // MeleeAttackBehaviourが攻撃するたびに更新する
    // BarkBehaviourがクールダウン判定に使用する
    // 初回攻撃前はクールダウンなし扱いにするためsentinel値で初期化する
    public float LastAttackTime = float.NegativeInfinity;
}
