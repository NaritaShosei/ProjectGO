using UnityEngine;

// TODO: Contextへの依存を削除
public class MeleeAttackBehaviour : IEnemyBehaviour
{
    public int Priority { get => (int)EnemyBehaviourPriority.Attack; }
    public bool CanEnter() { return CanAttack(); }
    public bool CanContinue() { return _isAttack; }

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

    public void OnEnter()
    {
        _isAttack = true;
    }

    public void Tick(float deltaTime)
    {
        if (!_isAttack) return;

        PerformAttack();

        Exit();
    }

    public void OnExit()
    {
        Exit();
    }

    private Transform _self;
    private Transform _player;
    private EnemyData _data;
    private EnemyContext _context;
    private EnemyStateContext _state;

    private float _lastAttackTime;

    private bool _isAttack;

    private bool CanAttack()
    {
        // Playerが不正ならリターン
        if (_player == null) { return false; }

        // 距離計算
        _context.DistanceToPlayer = Vector3.Distance(
            _self.position,
            _player.position
        );

        // Playerとの距離が遠いならリターン
        if (_context.DistanceToPlayer > _data.AttackRange) { return false; }

        // クールダウンを開けていなければリターン
        if (Time.time - _lastAttackTime < _data.AttackCooldown) { return false; }
        
        // 攻撃中であればリターン
        if(_isAttack) { return false; }

        return true;
    }

    private void PerformAttack()
    {
        _lastAttackTime = Time.time;

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

    private void Exit()
    {
        _isAttack = false;
    }
}
