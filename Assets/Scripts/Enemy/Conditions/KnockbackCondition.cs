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
    public KnockbackCondition(KnockbackContext context)
    {
        // 上方向には対応していない
        _velocity = context.Direction.normalized * context.Power;

        // ノックバック時間固定　
        // あとからいじれるようにするかはプランナーと相談
        _time = 0.2f;
    }

    public void OnEnter(IEnemy enemy)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("ノックバック開始");
#endif
    }

    public void Tick(IEnemy enemy, float deltaTime)
    {
        _time -= deltaTime;
        enemy.AddKnockBackForce(_velocity * deltaTime);
    }

    public void OnExit(IEnemy enemy)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("ノックバック終了");
#endif
    }

    private float _time;
    private readonly Vector3 _velocity;
}
