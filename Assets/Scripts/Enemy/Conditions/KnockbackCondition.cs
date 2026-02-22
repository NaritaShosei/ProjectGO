using Unity.Behavior;
using UnityEngine;

/// <summary>
/// 作ってみたけど、やり方としてはDoTweenで飛ばすか、
/// 飛ばすだけにして終了はEnemyが地面に着地したら、とかのほうがいいのか・・
/// まだAttackContextに対応できていないのでお待ちを・・
/// </summary>
public sealed class KnockbackCondition : IEnemyCondition
{
    public ConditionType Type => ConditionType.Knockback;
    public bool BlocksAction => true;
    public bool IsFinished => _time <= 0f;

    // Condition作成時にKnockbackの方向を計算
    public KnockbackCondition(Vector3 dir, float power)
    {
        _velocity = dir.normalized * power;

        // ノックバック時間固定　
        // あとからいじれるようにするかはプランナーと相談
        _time = 0.2f;
    }

    public void OnEnter(IEnemy enemy)
    {
        // TODO: IEnemyにVelocityを追加してノックバックを実行する
        // enemy.Velocity = _velocity;
    }

    public void Tick(IEnemy enemy, float dt)
    {
        _time -= dt;
        // enemy.transform.position += enemy.Velocity * dt;
    }

    public void OnExit(IEnemy enemy)
    {
        // enemy.Velocity = Vector3.zero;
    }

    private float _time;
    private readonly Vector3 _velocity;
}
