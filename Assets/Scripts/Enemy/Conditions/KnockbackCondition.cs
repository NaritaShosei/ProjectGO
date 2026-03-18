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
    public KnockbackCondition(KnockbackContext context, KnockbackLevel level)
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

        if (_level == KnockbackLevel.Large)
        {
            // 死亡後のAnimationEvent発火に備えてOnDeadを購読する
            _enemy = enemy;
            _enemy.OnDead += HandleEnemyDead;

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

            if (_level == KnockbackLevel.Large)
            {
                if (_enemyAnimator == null)
                {
                    _isFinished = true;
                }
                else
                {
                    _landingDone = true;
                }
            }
            else
            {
                _isFinished = true;
            }
        }
    }

    public void OnExit(IEnemy enemy)
    {
        // Level Large の購読をまとめて解除する
        if (_enemy != null)
        {
            _enemy.OnDead -= HandleEnemyDead;
            _enemy = null;
        }

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
    private IEnemyAnimator _enemyAnimator;
    private readonly KnockbackLevel _level;

    // Level Large の死亡後発火ガード用
    private IEnemy _enemy;
    private bool _enemyDead;

    private const float _gravity = -30f; // あえて大きめに

    /// <summary>
    /// 敵死亡通知のハンドラ
    /// GetUpEnd が死亡後に発火した場合のガードに使用する
    /// </summary>
    private void HandleEnemyDead(IEnemy _)
    {
        _enemyDead = true;
    }

    /// <summary>
    /// GetUpアニメーション完了時に呼ばれるハンドラ
    /// 敵がすでに死亡している場合はCondition終了処理をスキップする
    /// </summary>
    private void HandleGetUpEnd()
    {
        if (_enemyDead) return;
        _isFinished = true;
    }
}

/// <summary>
/// ノックバックのレベルを表すenum
/// KnockbackContext.Power をもとに MobEnemy.DetermineKnockbackLevel() で決定される
/// </summary>
public enum KnockbackLevel
{
    /// <summary>ヒットリアクション（移動なし）</summary>
    Hit = 0,
    /// <summary>小ノックバック（小移動）</summary>
    Small = 1,
    /// <summary>大ノックバック（大移動 → GetUp）</summary>
    Large = 2,
}
