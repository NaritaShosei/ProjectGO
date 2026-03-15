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
        EnemyAnimator enemyAnimator,
        Animator animator,
        EnemyStateContext state
    )
    {
        _self = owner.transform;
        _enemyId = owner.GetInstanceID();
        _player = player;
        _data = data;
        _context = context;
        _enemyAnimator = enemyAnimator;
        _state = state;
        _animator = animator;
    }

    public bool CanEnter()
    {
        if (_player == null) return false;
        if (_attackerSlot == null) return false;

        if (_isAttacking) return false;

        if (Time.time - _lastAttackTime < _data.AttackCooldown) return false;

        int slotCost = _data.AttackPattern != null
            ? _data.AttackPattern.SlotCost
            : 1;

        // スロット確保を先に試みる（射程外でも確保することでMoveが動く）
        // 確保済みの場合は即trueが返る
        // 満杯の場合はfalseが返り、Bark/Roamへフォールバックする
        if (!_attackerSlot.TryAcquire(_enemyId, slotCost, isBoss: false)) return false;

        // スロット確保後に射程チェック
        // 射程外の場合はfalseを返すが、スロットは確保済みのままなのでMoveが動く
        _context.DistanceToPlayer = Vector3.Distance(_self.position, _player.position);
        return _context.DistanceToPlayer <= _data.AttackRange;
    }

    public bool CanContinue()
    {
        return _isAttacking;
    }

    public void OnEnter()
    {
        // スロットはCanEnterで確保済みのためここでは確保しない
        _isAttacking = true;
        _timer = 0f;
        _state.ChangeState(EnemyState.Attack);

        _enemyAnimator?.SetAttacking(true);

        PerformAttack();
    }

    public void Tick(float deltaTime)
    {
        if (!_isAttacking) return;

        _timer += deltaTime;

        // AttackPatternが設定されている場合はDurationで終了判定
        // 設定されていない場合は即終了
        float duration = (_data.AttackPattern != null && _data.AttackPattern.Duration > 0f)
            ? _data.AttackPattern.Duration
            : GetAnimationLength();

        if (_timer >= duration)
        {
            Exit();
        }
    }

    public void OnExit()
    {
        _enemyAnimator?.SetAttacking(false);
        Exit();
    }

    private Transform _self;
    private Transform _player;
    private EnemyData _data;
    private EnemyContext _context;
    private EnemyStateContext _state;
    private readonly IEnemyAttackerSlot _attackerSlot;
    private EnemyAnimator _enemyAnimator;

    private int _enemyId;
    private float _lastAttackTime;
    private float _timer;
    private bool _isAttacking;
    private Animator _animator;

    // AttackステートのAnimatorハッシュ
    private static readonly int _attackStateHash = Animator.StringToHash("Attack");

    private void PerformAttack()
    {
        _lastAttackTime = Time.time;

        Debug.Log($"Enemyが攻撃した");

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
    // 死亡時にEnemyから明示的に呼ぶ
    public void ReleaseSlot()
    {
        if (_attackerSlot == null) return;

        int slotCost = _data.AttackPattern != null
            ? _data.AttackPattern.SlotCost
            : 1;

        _attackerSlot.Release(_enemyId, slotCost);
    }

    private void Exit()
    {
        if (!_isAttacking) return;

        _isAttacking = false;
        _state.ChangeState(EnemyState.Idle);

        // スロットは死亡時にのみ解放するため、ここでは解放しない
    }

    /// <summary>
    /// AttackステートのAnimationClip長を取得する
    /// AttackPattern.Durationが未設定の場合のフォールバック
    /// </summary>
    private float GetAnimationLength()
    {
        if (_animator == null) return 0f;

        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);

        if (stateInfo.IsName("Attack"))
        {
            // normalizedTimeが1に達したらClip1周分
            return stateInfo.length;
        }

        // Attackステートに遷移前の場合は次フレームまで待つ
        return float.MaxValue;
    }
}
