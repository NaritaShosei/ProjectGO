using UnityEngine;

// NOTE:
// この GoblinEnemy は「基盤用の最小実装」です。
// ・複雑なAI
// ・スキル
// ・状態遷移
// は意図的に入れていません。
// 拡張する場合はこのクラスを参考に派生 or 分離してください。

public class GoblinEnemy : Enemy
{
    public override void Init(IPlayer player)
    {
        base.Init(player);

        _context = new EnemyContext();
        _runner = new EnemyBehaviourRunner(this);
        _state = new EnemyStateContext();

        // TurnProfileが未設定の場合は警告を出してTurnを登録しない
        if (_turnProfile == null)
        {
            Debug.LogWarning($"{nameof(GoblinEnemy)}: TurnProfileが未設定です。Turnは無効になります。");
        }
        else
        {
            _turn = new TurnBehaviour(_turnProfile);
            _turn.Init(this, _data, _playerTransform, _context, _state);
            _runner.RegisterTurn(_turn);
        }

        // AttackerSlotが未設定の場合は警告を出してAttackを登録しない
        if (_attackerSlot == null)
        {
            Debug.LogWarning($"{nameof(GoblinEnemy)}: AttackerSlotが未注入です。Attackは無効になります。");
        }
        else
        {
            _attack = new MeleeAttackBehaviour(_attackerSlot);
            _attack.Init(this, _data, _playerTransform, _context, _enemyAnimator, _animator, _state);
            _runner.Register(_attack);

            // スポーン時にスロット取得を試みる
            // 満杯の場合は OnSlotReleased イベントで再試行される
            int goblinSlotCost = _data.AttackPattern != null ? _data.AttackPattern.SlotCost : 1;
            _attackerSlot.TryAcquire(GetInstanceID(), goblinSlotCost, isBoss: false);

            // BarkをattackerSlotブロック内に移動
            // distanceProfileがない場合はBarkも登録しない
            if (_distanceProfile != null)
            {
                _bark = new BarkBehaviour(_attackerSlot, _data.BarkChance);
                _bark.Init(this, _data, _playerTransform, _context, _enemyAnimator, _state);
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
            var move = new MoveBehaviour(
                _distanceProfile,
                _attackerSlot,
                _separationService,
                _wallAvoidanceService,
                _spatialHashGrid
            );
            move.Init(this, _data, _playerTransform, _context, _enemyAnimator, _state);
            _runner.Register(move);

            // BarkはattackerSlotブロックへ移動したためここから削除

            var roam = new RoamBehaviour(
                _distanceProfile,
                _separationService,
                _wallAvoidanceService,
                _spatialHashGrid,
                dir => _turn?.SetOverrideDirection(dir)
            );
            roam.Init(this, _data, _playerTransform, _context, _enemyAnimator, _state);
            _runner.Register(roam);
        }
    }

    private EnemyBehaviourRunner _runner;
    private EnemyContext _context;
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

        _runner.Tick(deltaTime);
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
    // デバッグ用にシーンビューで球体を描く
    private void OnDrawGizmosSelected()
    {
        if (_data == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(
            transform.position + transform.forward * _data.AttackRange,
            _data.AttackRadius
        );
    }
#endif
}
