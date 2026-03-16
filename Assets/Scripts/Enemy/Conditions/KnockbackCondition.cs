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

    /// <param name="level">ノックバックレベル（0=Hit / 1=Small / 2=Large）</param>
    public KnockbackCondition(KnockbackContext context, int level)
    {
        var horizontal = context.Direction.normalized * context.Power;
        var vertical = Vector3.up * context.Upward;

        _velocity = horizontal + vertical;
        _level = level;
    }

    public void OnEnter(IEnemy enemy)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("ノックバック開始");
#endif
        _groundY = enemy.Position.y;
        _isFinished = false;
        _landingDone = false;

        // level 2（Large）はGetUp完了まで待機するためAnimatorを保持する
        if (_level == 2)
        {
            _enemyAnimator = enemy.EnemyAnimator;
            if (_enemyAnimator != null)
            {
                _enemyAnimator.OnGetUpEnd += HandleGetUpEnd;
            }
        }

        enemy.EnemyAnimator?.SetKnockback(true, _level);
    }

    public void Tick(IEnemy enemy, float deltaTime)
    {
        // 着地済みの場合は移動処理をスキップする（level 2 のGetUp待機中）
        if (_landingDone) return;

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

            // level 2（Large）はGetUpEnd イベントを待ってから終了する
            if (_level == 2)
            {
                _landingDone = true;
            }
            else
            {
                _isFinished = true;
            }
        }
    }

    public void OnExit(IEnemy enemy)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("ノックバック終了");
#endif
        // level 2（Large）の購読解除
        if (_enemyAnimator != null)
        {
            _enemyAnimator.OnGetUpEnd -= HandleGetUpEnd;
            _enemyAnimator = null;
        }

        enemy.EnemyAnimator?.SetKnockback(false);
    }

    private Vector3 _velocity;
    private float _groundY;
    private bool _isFinished;
    private bool _landingDone;

    // level 2（Large）のGetUpEnd購読解除用
    private EnemyAnimator _enemyAnimator;
    private readonly int _level;

    private const float _gravity = -30f; // あえて大きめに

    /// <summary>
    /// GetUpアニメーション完了時に呼ばれるハンドラ
    /// level 2（Large）のCondition終了をここで確定させる
    /// </summary>
    private void HandleGetUpEnd()
    {
        _isFinished = true;
    }
}
