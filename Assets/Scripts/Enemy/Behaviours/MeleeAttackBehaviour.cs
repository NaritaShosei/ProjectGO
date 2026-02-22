using UnityEngine;

// TODO: Contextへの依存を削除
public class MeleeAttackBehaviour : IEnemyBehaviour
{
    public int Priority { get => (int)EnemyBehaviourPriority.Attack; }
    public bool CanEnter() { return CanAttack(); }
    public bool CanContinue() { return _state.CurrentState == EnemyState.Attack; }

    public void OnEnter() { }
    public void OnExit() { }

    public void Init(
        Enemy owner,
        EnemyData data,
        Transform player,
        EnemyContext context, 
        EnemyStateContext state
    )
    {
        _self = owner.transform;
        _player = player;
        _data = data;
        _context = context;
        _state = state;
    }

    public void Tick(float deltaTime)
    {
        // 距離計算
        _context.DistanceToPlayer = Vector3.Distance(
            _self.position,
            _player.position
        );

        // 攻撃を実行
        _state.ChangeState(EnemyState.Attack);
        _lastAttackTime = Time.time;

        PerformAttack();

        // TODO:IsAttackingが1フレーム内でリセットされているのでアニメーションなどに対応させる必要あり
        _state.ChangeState(EnemyState.Idle);
    }

    private bool CanAttack()
    {
        if (_player == null) { return false; }

        // 攻撃の条件に満たしていなかったら早期リターン
        if (!_state.CanAttack()) { return false; }

        // 距離計算
        _context.DistanceToPlayer = Vector3.Distance(
            _self.position,
            _player.position
        );

        // 攻撃の条件に満たしていなかったら早期リターン
        if (_context.DistanceToPlayer > _data.AttackRange) { return false; }
        if (Time.time - _lastAttackTime < _data.AttackCooldown) { return false; }
        
        return true;
    }

    private void PerformAttack()
    {
        // 球体をつくり、その範囲内にいるPlayerに攻撃
        Collider[] hits = Physics.OverlapSphere(
            _self.position + _self.forward * _data.AttackRange,
            _data.AttackRadius
        );

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out IPlayer player))
            {
                player.TakeDamage(_data.AttackDamage);
            }
        }
    }

    private Transform _self;
    private Transform _player;
    private EnemyData _data;
    private EnemyContext _context;
    private EnemyStateContext _state;

    private float _lastAttackTime;


}
