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
            var turn = new TurnBehaviour(_turnProfile);
            turn.Init(this, _data, _playerTransform, _context, _state);
            _runner.RegisterTurn(turn);
        }

        // AttackerSlotが未設定の場合は警告を出してAttackを登録しない
        if (_attackerSlot == null)
        {
            Debug.LogWarning($"{nameof(GoblinEnemy)}: AttackerSlotが未注入です。Attackは無効になります。");
        }
        else
        {
            var attack = new MeleeAttackBehaviour(_attackerSlot);
            attack.Init(this, _data, _playerTransform, _context, _state);
            _runner.Register(attack);
        }

        var move = new MoveBehaviour(
            _distanceProfile,
            _separationService,
            _wallAvoidanceService,
            _spatialHashGrid
        );
        move.Init(this, _data, _playerTransform, _context, _state);
        _runner.Register(move);

        var roam = new RoamBehaviour(
            _distanceProfile,
            _separationService,
            _wallAvoidanceService,
            _spatialHashGrid
        );
        roam.Init(this, _data, _playerTransform, _context, _state);
        _runner.Register(roam);

        var bark = new BarkBehaviour(_distanceProfile);
        bark.Init(this, _data, _playerTransform, _context, _state);
        _runner.Register(bark);

    }

    private EnemyBehaviourRunner _runner;
    private EnemyContext _context;
    private EnemyStateContext _state;

    public override void OnConditionInterrupt()
    {
        _runner.ForceExitAction();
    }

    protected override void UpdateEnemy(float deltaTime)
    {
        if (_runner == null) return;

        _runner.Tick(deltaTime);
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
