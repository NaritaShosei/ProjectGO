public class EnemyContext
{
    public bool IsAttacking { get; set; }
    public bool CanMove => !IsAttacking;

    public float DistanceToPlayer;
}
