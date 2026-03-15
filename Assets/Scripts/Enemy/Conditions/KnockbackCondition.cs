using Unity.Behavior;
using UnityEngine;

/// <summary>
/// 斜め移動の挙動を手計算で実装
/// 設計思想
/// ・EnemyにRigidBodyがないこと
/// ・NavMeshをつかわないこと
/// ・自分の高さの確認のためLayCastを使用しないこと
/// </summary>
public sealed class KnockbackCondition : IEnemyCondition
{
    public ConditionType Type => ConditionType.Knockback;
    public bool BlocksAction => true;
    public bool IsFinished => _isFinished;

    // Condition作成時にKnockbackの方向を計算
    public KnockbackCondition(KnockbackContext context)
    {
        var horizontal = context.Direction.normalized * context.Power;
        var vertical = Vector3.up * context.Upward;

        _velocity = horizontal + vertical;
    }

    public void OnEnter(IEnemy enemy)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("ノックバック開始");
#endif
        _groundY = enemy.Position.y; 
        _isFinished = false;
        enemy.EnemyAnimator?.SetKnockback(true);
    }

    public void Tick(IEnemy enemy, float deltaTime)
    {
        // 重力
        _velocity.y += _gravity * deltaTime;

        Vector3 delta = _velocity * deltaTime;
        enemy.AddKnockBackForce(delta);

        // 着地判定
        if (enemy.Position.y <= _groundY)
        {
            var pos = enemy.Position;
            pos.y = _groundY;
            enemy.SetPosition(pos);

            _isFinished = true;
        }
    }

    public void OnExit(IEnemy enemy)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("ノックバック終了");
#endif
        enemy.EnemyAnimator?.SetKnockback(false);
    }

    private Vector3 _velocity;
    private float _groundY;
    private bool _isFinished;
    private const float _gravity = -30f; // あえて大きめに
}
