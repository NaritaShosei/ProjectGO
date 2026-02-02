public class EnemyContext
{

    // TODO: ここにスタンしているというboolを追加する。
    // TODO: スタンしている間は行動不能にする。

    public bool IsAttacking { get; set; }
    public bool CanMove => !IsAttacking;

    public float DistanceToPlayer;
}
