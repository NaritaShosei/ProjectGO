using UnityEngine;

/// <summary>
/// ゴブリン敵の実装クラス
/// MobEnemyと同等のBehaviour構成を持つが、鎧・感電などの追加要素を含まないシンプルな実装
/// </summary>
public class GoblinEnemy : Enemy
{
    public override void Init()
    {
        _context = new EnemyRuntimeContext();
        _runner = new EnemyBehaviourRunner(this);
        _state = new EnemyStateContext();

        var initCtx = new BehaviourInitContext(this, _data, _playerTransform, _context, _enemyAnimator, _state);

        // TurnProfileが未設定の場合は警告を出してTurnを登録しない
        if (_turnProfile == null)
        {
            Debug.LogWarning($"{nameof(GoblinEnemy)}: TurnProfileが未設定です。Turnは無効になります。");
        }
        else
        {
            _turn = new TurnBehaviour(_turnProfile);
            _turn.Init(initCtx);
            _runner.RegisterTurn(_turn);
        }

        // AttackerSlotが未設定の場合は警告を出してAttackを登録しない
        if (_services.AttackerSlot == null)
        {
            Debug.LogWarning($"{nameof(GoblinEnemy)}: AttackerSlotが未注入です。Attackは無効になります。");
        }
        else if (_data.AttackPatterns == null || _data.AttackPatterns.Count == 0)
        {
            Debug.LogWarning($"{nameof(GoblinEnemy)}: AttackPatternsが空です。Attack・スロット取得をスキップします。");
        }
        else
        {
            _attack = new MeleeAttackBehaviour(_services, _animator, _distanceProfile);
            _attack.Init(initCtx);
            _runner.Register(_attack);

            // スポーン時にスロット取得を試みる
            // 満杯の場合は OnSlotReleased イベントで再試行される
            _services.AttackerSlot.TryAcquire(Id, 1);

            // BarkをattackerSlotブロック内に移動
            // distanceProfileがない場合はBarkも登録しない
            if (_distanceProfile != null)
            {
                _bark = new BarkBehaviour(_distanceProfile, _services, _data.BarkChance);
                _bark.Init(initCtx);
                _runner.Register(_bark);
            }
        }

        // DistanceProfileが未設定の場合は警告を出してMove・Bark・Roamを登録しない
        if (_distanceProfile == null)
        {
            Debug.LogWarning($"{nameof(GoblinEnemy)}: DistanceProfileが未設定です。Approach・Bark・Roamは無効になります。");
        }
        else
        {
            var move = new ApproachBehaviour(_distanceProfile, _services);
            move.Init(initCtx);
            _runner.Register(move);

            var roam = new RoamBehaviour(
                _distanceProfile,
                _services,
                dir => _turn?.SetOverrideDirection(dir)
            );
            roam.Init(initCtx);
            _runner.Register(roam);

            var idle = new IdleBehaviour();
            idle.Init(initCtx);
            _runner.Register(idle);
        }
    }

    private EnemyBehaviourRunner _runner;
    private EnemyRuntimeContext _context;
    private EnemyStateContext _state;
    private MeleeAttackBehaviour _attack;
    private BarkBehaviour _bark;
    private TurnBehaviour _turn;

    public override void OnConditionInterrupt()
    {
        _runner.ForceExitAction();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        _bark?.Dispose();
        _attack?.Dispose();
    }

    protected override void UpdateEnemy(float deltaTime)
    {
        if (_runner == null) return;

        // 攻撃クールダウンをTimeScale反映済みdeltaTimeで進める
        if (_context.AttackCooldownRemaining > 0f)
        {
            _context.AttackCooldownRemaining -= deltaTime;
            if (_context.AttackCooldownRemaining < 0f) _context.AttackCooldownRemaining = 0f;
        }

        // スロット保持中にパターン未選択なら再選択する
        if (_services.AttackerSlot != null && _services.AttackerSlot.IsAcquired(Id) && _context.SelectedPattern == null)
        {
            _context.SelectedPattern = SelectPattern();
        }

        _runner.Tick(deltaTime);
    }

    private EnemyAttackPattern SelectPattern()
    {
        if (_data.AttackPatterns == null || _data.AttackPatterns.Count == 0) return null;
        return _data.AttackPatterns[UnityEngine.Random.Range(0, _data.AttackPatterns.Count)];
    }

    public override void TakeDamage(DamageContext context)
    {
        if (context.Knockback != null)
            _lastHitDirection = ((KnockbackContext)context.Knockback).Direction;

        base.TakeDamage(context);
    }

    /// <summary>
    /// スロット解放・Behaviourの停止を行う。
    /// 死亡時と将来のプール返却時（OnDespawn）の両方から呼ぶ想定。
    /// </summary>
    protected virtual void OnDespawn()
    {
        _runner?.ForceExitAction();
        _attack?.ReleaseSlot();
    }

    protected override void OnDeathInternal()
    {
        OnDespawn();

        // SetDead() と物理ノックバックを DeadCondition に委譲する
        new DeadCondition(_lastHitDirection, _data, destroyCancellationToken).OnEnter(this);

        base.OnDeathInternal();
    }

#if UNITY_EDITOR
    // Attacker取得中の敵の頭上にマーカーを常時表示する
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        if (_services.AttackerSlot == null) return;
        if (!_services.AttackerSlot.IsAcquired(Id)) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position + Vector3.up * 2.5f, 0.2f);
    }

    // デバッグ用にシーンビューで球体を描く
    private void OnDrawGizmosSelected()
    {
        if (_data == null) return;

        var pattern = _data.AttackPatterns?.Count > 0 ? _data.AttackPatterns[0] : null;
        if (pattern == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(
            transform.position + transform.forward * pattern.AttackRange,
            pattern.AttackRadius
        );
    }
#endif
}
