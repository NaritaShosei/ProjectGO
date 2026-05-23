using UnityEngine;

/// <summary>
/// ノックバック移動を手計算で実装
/// 設計思想
/// ・EnemyにRigidBodyがないこと
/// ・NavMeshをつかわないこと
/// ・自分の高さの確認のためLayCastを使用しないこと
/// ・水平（_velocityH）と垂直（_velocityV）を分離することでUpward=0でも正常動作する
/// </summary>
public sealed class KnockbackCondition : IEnemyCondition
{
    public ConditionType Type => ConditionType.Knockback;
    public bool BlocksAction => true;
    public bool IsFinished => _isFinished;

    /// <summary>
    /// ノックバック条件を初期化する
    /// </summary>
    /// <param name="level">ノックバックレベル（0=Hit / 1=Small / 2=Large）</param>
    /// <param name="stunDuration">停止後の硬直時間（秒）Hit/Small のみ使用</param>
    /// <param name="deceleration">水平方向の減速度（単位/秒²）飛距離 ≈ Power² / (2 × deceleration)</param>
    public KnockbackCondition(KnockbackContext context, KnockbackLevel level, float stunDuration, float deceleration)
    {
        // Direction の Y 成分を除き水平成分のみ使用する
        var dirXZ = new Vector3(context.Direction.x, 0f, context.Direction.z);
        if (dirXZ.sqrMagnitude > 0f)
            dirXZ = dirXZ.normalized;

        _velocityH = dirXZ * context.Power;
        _velocityV = context.Upward;
        _level = level;
        _stunDuration = stunDuration;
        _deceleration = deceleration;
    }

    public void OnEnter(IEnemy enemy)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("ノックバック開始");
#endif
        _groundY = enemy.Self.position.y;
        _isFinished = false;
        _landingDone = false;
        _stunRemaining = 0f;

        _enemyAnimator = enemy.EnemyAnimator;

        if (_level == KnockbackLevel.Large)
        {
            // 死亡後のAnimationEvent発火に備えてOnDeadを購読する
            _enemy = enemy;
            _enemy.OnDead += HandleEnemyDead;

            if (_enemyAnimator != null)
            {
                _enemyAnimator.OnGetUpEnd += HandleGetUpEnd;
            }
        }
        else
        {
            // Hit / Small: アニメーション終了で条件を終わらせる
            if (_enemyAnimator != null)
            {
                _enemyAnimator.OnKnockbackEnd += HandleKnockbackEnd;
            }
        }

        enemy.EnemyAnimator?.SetKnockback(true, _level);

        // Hit レベルは移動なし: 即スタンカウントダウンへ
        if (_level == KnockbackLevel.Hit)
        {
            _landingDone = true;
            _stunRemaining = _stunDuration;
        }
    }

    public void Tick(IEnemy enemy, float deltaTime)
    {
        // 停止済み: Large → GetUp 待機 / Hit・Small → アニメーション終了待ち
        if (_landingDone)
        {
            if (_level == KnockbackLevel.Large) return;

            // スタンカウントダウン（最低保証時間）
            if (_stunRemaining > 0f)
                _stunRemaining -= deltaTime;

            // スタン終了 かつ アニメーション終了で条件を終わらせる
            if (_stunRemaining <= 0f && _animEnd)
                _isFinished = true;

            return;
        }

        // 水平減速
        float hSpeed = _velocityH.magnitude;
        if (hSpeed > 0f)
        {
            float newSpeed = Mathf.Max(0f, hSpeed - _deceleration * deltaTime);
            _velocityH = newSpeed > 0f ? _velocityH.normalized * newSpeed : Vector3.zero;
        }

        // 垂直（重力）
        _velocityV += _gravity * deltaTime;

        // 移動適用
        Vector3 delta = _velocityH * deltaTime + Vector3.up * (_velocityV * deltaTime);
        enemy.AddKnockbackForce(delta);

        // 地面クランプ（めり込み防止）
        if (enemy.Self.position.y < _groundY)
        {
            var pos = enemy.Self.position;
            pos.y = _groundY;
            enemy.SetPosition(pos);
            _velocityV = 0f;
        }

        // 停止判定（地面に接触 かつ 水平速度がゼロ）
        if (enemy.Self.position.y <= _groundY && _velocityH.sqrMagnitude < 0.0001f)
        {
            _velocityH = Vector3.zero;
            _landingDone = true;

            if (_level == KnockbackLevel.Large)
            {
                if (_enemyAnimator == null)
                    _isFinished = true;
                // else: HandleGetUpEnd 待ち
            }
            else
            {
                // 停止後、硬直カウントダウンに入る（アニメーション終了も待つ）
                _stunRemaining = _stunDuration;
                // アニメーションが先に終わっていた場合は即チェック
                if (_stunRemaining <= 0f && _animEnd)
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
            _enemyAnimator.OnKnockbackEnd -= HandleKnockbackEnd;
            _enemyAnimator = null;
        }

        enemy.EnemyAnimator?.SetKnockback(false);
    }

    // 水平速度（XZ平面、減速制御）
    private Vector3 _velocityH;
    // 垂直速度（Y軸、重力制御）
    private float _velocityV;
    private float _groundY;
    private float _stunRemaining;
    private readonly float _stunDuration;
    private readonly float _deceleration;
    private bool _isFinished;
    private bool _landingDone;
    // Hit / Small: アニメーション終了フラグ
    private bool _animEnd;

    // イベント購読解除用
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
    /// GetUpアニメーション完了時に呼ばれるハンドラ（Large のみ使用）
    /// 敵がすでに死亡している場合はCondition終了処理をスキップする
    /// </summary>
    private void HandleGetUpEnd()
    {
        if (_enemyDead) return;
        _isFinished = true;
    }

    /// <summary>
    /// Knockback_Hit / Small アニメーション完了時に呼ばれるハンドラ
    /// 移動も停止済みの場合に条件を終了する
    /// </summary>
    private void HandleKnockbackEnd()
    {
        _animEnd = true;
        if (_landingDone && _stunRemaining <= 0f)
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
