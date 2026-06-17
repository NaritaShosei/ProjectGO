/// <summary>
/// ダウン状態のCondition
/// BlocksActionがtrueのため、発動中はEnemyの行動をすべて停止する
/// OnEnter/OnExit は未実装（アニメーション・鎧回復などは今後対応予定）
/// </summary>
public sealed class DownCondition : IEnemyCondition
{
    public ConditionType Type => ConditionType.Down;
    public bool BlocksAction => true;

    public bool IsFinished => _time <= 0f;

    public DownCondition(float duration)
    {
        _time = duration;
    }

    public void OnEnter(IEnemy enemy)
    {
        // TODO: EnemyでDownを開始させる
        // 具体的にはアニメーションの開始や被ダメージ量アップ状態の登録
        enemy.EnemyAnimator?.SetDown(true);
        enemy.EnemyAnimator?.DownTrigger();
    }

    public void Tick(IEnemy enemy, float dt)
    {
        _time -= dt;
    }

    public void OnExit(IEnemy enemy)
    {
        // TODO: EnemyでDownから回復させる
        // 具体的にはアーマーの回復
        enemy.EnemyAnimator?.SetDown(false);
        if (enemy is GolemEnemy golem)
        {
            golem.RecoverArmor();
        }
    }

    private float _time;
}
