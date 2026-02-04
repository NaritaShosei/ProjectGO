public class EnemyContext
{
    // TODO: Enemyの状態変化をEnemyStateManagerのほうへ統合したい
    // TODO: 最終的にこのクラスは削除したい
    // TODO: もしくはここにEnemyStateの変数を保持する方針でも可。

    public bool IsAttacking { get; set; }
    public bool CanMove => !IsAttacking;

    public float DistanceToPlayer;
}
