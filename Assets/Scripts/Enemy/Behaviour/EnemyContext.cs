public class EnemyContext
{
    // TODO: IsAttacking, CanMoveはこちらから削除してEnemyStateManagerに移したい
    public bool IsAttacking { get; set; }
    public bool CanMove => !IsAttacking;

    public float DistanceToPlayer;
}
