using UnityEngine;

/// <summary>
/// Enemyの基盤用最小実装クラス
/// 複雑なAI・スキル・状態遷移は意図的に含めていない
/// 拡張する場合はこのクラスを参考に派生 or 分離してください
/// </summary>
public class GoblinEnemy : Enemy
{
    public override void Init(IPlayer player)
    {
        base.Init(player);

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
        else
        {
            _attack = new MeleeAttackBehaviour(_services, _animator, _distanceProfile);
            _attack.Init(initCtx);
            _runner.Register(_attack);

            // スポーン時にスロット取得を試みる
            // 満杯の場合は OnSlotReleased イベントで再試行される
            int goblinSlotCost = _data.AttackPatterns?.Count > 0 ? _data.AttackPatterns[0].SlotCost : 1;
            _context.AcquiredSlotCost = goblinSlotCost;
            _services.AttackerSlot.TryAcquire(Id, goblinSlotCost, isBoss: false);

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
            Debug.LogWarning($"{nameof(GoblinEnemy)}: DistanceProfileが未設定です。Move・Bark・Roamは無効になります。");
        }
        else
        {
            var move = new MoveBehaviour(_distanceProfile, _services);
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

    protected override void OnDeathInternal()
    {
        _runner?.ForceExitAction();

        // 死亡時にスロットを解放する
        _attack?.ReleaseSlot();

        // 死亡アニメーションを再生する
        _enemyAnimator?.SetDead();

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
