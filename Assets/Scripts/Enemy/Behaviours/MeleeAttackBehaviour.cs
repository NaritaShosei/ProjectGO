using UnityEngine;

/// <summary>
/// 近接攻撃Behaviour
/// AttackerSlotでスロットを確保できた場合のみ攻撃する
/// AttackPatternのDurationでモーション時間を管理する
/// </summary>
public class MeleeAttackBehaviour : IEnemyBehaviour
{
    public int Priority { get => (int)EnemyBehaviourPriority.Attack; }

    /// <summary>
    /// AttackerSlotはMeleeAttackBehaviour固有の依存のためコンストラクタで受け取る
    /// </summary>
    public MeleeAttackBehaviour(IEnemyAttackerSlot attackerSlot)
    {
        _attackerSlot = attackerSlot;
    }

    public void Init(
        Enemy owner,
        EnemyData data,
        Transform player,
        EnemyContext context,
        EnemyStateContext state
    )
    {
        _self = owner.transform;
        _enemyId = owner.GetInstanceID();
        _player = player;
        _data = data;
        _context = context;
        _state = state;
    }

    public bool CanEnter()
    {
        // Playerが不正ならリターン
        if (_player == null) return false;

        // 距離計算
        _context.DistanceToPlayer = Vector3.Distance(
            _self.position,
            _player.position
        );

        // Playerとの距離が遠いならリターン
        if (_context.DistanceToPlayer > _data.AttackRange) return false;

        // クールダウンが明けていなければリターン
        if (Time.time - _lastAttackTime < _data.AttackCooldown) return false;

        // 攻撃中ならリターン
        if (_isAttacking) return false;

        return true;
    }

    public bool CanContinue()
    {
        return _isAttacking;
    }

    public void OnEnter()
    {
        // スロットが未設定ならリターン
        if (_attackerSlot == null) return;

        int slotCost = _data.AttackPattern != null
            ? _data.AttackPattern.SlotCost
            : 1;

        // スロットが確保できなければクールダウンを更新してリターン
        // 更新しないと毎フレームOnEnterが呼ばれ続けてしまう
        if (!_attackerSlot.TryAcquire(_enemyId, slotCost, isBoss: false))
        {
            _lastAttackTime = Time.time;
            return;
        }

        _isAttacking = true;
        _timer = 0f;
        _state.ChangeState(EnemyState.Attack);

        PerformAttack();
    }

    public void Tick(float deltaTime)
    {
        if (!_isAttacking) return;

        _timer += deltaTime;

        // AttackPatternが設定されている場合はDurationで終了判定
        // 設定されていない場合は即終了
        float duration = _data.AttackPattern != null
            ? _data.AttackPattern.Duration
            : 0f;

        if (_timer >= duration)
        {
            Exit();
        }
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
    private readonly IEnemyAttackerSlot _attackerSlot;

    private int _enemyId;
    private float _lastAttackTime;
    private float _timer;
    private bool _isAttacking;

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
        // 二重呼び出し防止
        if (!_isAttacking) return;

        _isAttacking = false;
        _state.ChangeState(EnemyState.Idle);

        // スロットを解放する
        if (_attackerSlot == null) return;

        int slotCost = _data.AttackPattern != null
            ? _data.AttackPattern.SlotCost
            : 1;

        _attackerSlot.Release(_enemyId, slotCost);
    }
}
