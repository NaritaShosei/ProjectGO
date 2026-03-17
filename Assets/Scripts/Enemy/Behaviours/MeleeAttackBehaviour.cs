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
        IEnemyAnimator enemyAnimator,
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

        // AttackHit / AttackEndイベントを購読してアニメーションと同期する
        if (_enemyAnimator != null)
        {
            _enemyAnimator.OnAttackHit += HandleAttackHit;
            _enemyAnimator.OnAttackEnd += HandleAttackEnd;
        }
    }

    /// <summary>
    /// イベント購読を解除する
    /// </summary>
    public void Dispose()
    {
        if (_enemyAnimator != null)
        {
            _enemyAnimator.OnAttackHit -= HandleAttackHit;
            _enemyAnimator.OnAttackEnd -= HandleAttackEnd;
        }
    }

    public bool CanEnter()
    {
        if (_player == null) return false;
        if (_attackerSlot == null) return false;
        if (_isAttacking) return false;

        // スポーン時に確保済みのスロットを持っているかチェック
        if (!_attackerSlot.IsAcquired(_enemyId)) return false;

        // クールダウン判定をEnemyContext.LastAttackTimeで行う
        if (Time.time - _context.LastAttackTime < _data.AttackCooldown) return false;

        // 射程チェック
        _context.DistanceToPlayer = Vector3.Distance(_self.position, _player.position);
        return _context.DistanceToPlayer <= _data.AttackRange;
    }

    public bool CanContinue()
    {
        return _isAttacking;
    }

    public void OnEnter()
    {
        _isAttacking = true;
        _timer = 0f;
        _attackHitFired = false;
        _attackEndFired = false;
        _state.ChangeState(EnemyState.Attack);
        _enemyAnimator?.SetAttacking(true);
    }

    public void Tick(float deltaTime)
    {
        if (!_isAttacking) return;

        _timer += deltaTime;

        // AnimationEventで終了を検知できなかった場合のフォールバック
        // Duration / clip長を超えた場合は強制終了する
        if (!_attackEndFired)
        {
            float duration = (_data.AttackPattern != null && _data.AttackPattern.Duration > 0f)
                ? _data.AttackPattern.Duration
                : GetAnimationLength();

            if (_timer >= duration)
            {
                Exit();
            }
        }
    }

    public void OnExit()
    {
        if (!_isAttacking) return;
        Exit();
    }

    private Transform _self;
    private Transform _player;
    private EnemyData _data;
    private EnemyContext _context;
    private EnemyStateContext _state;
    private readonly IEnemyAttackerSlot _attackerSlot;
    private IEnemyAnimator _enemyAnimator;

    private int _enemyId;
    private float _timer;
    private bool _isAttacking;
    private bool _attackHitFired;
    private bool _attackEndFired;

    private Animator _animator;

    private void PerformAttack()
    {
# if UNITY_EDITOR
        Vector3 center = _self.position + _self.forward * _data.AttackRange;
        Debug.Log($"[Attack] center={center}, radius={_data.AttackRadius}, forward={_self.forward}");
        Collider[] debugHits = Physics.OverlapSphere(center, _data.AttackRadius);
        foreach (var h in debugHits)
        {
            Debug.Log($"[Attack] hit={h.gameObject.name}, hasIPlayer={h.TryGetComponent<IPlayer>(out _)}");
        }
# endif

        // クールダウン管理のためEnemyContextに記録する
        _context.LastAttackTime = Time.time;

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
        _enemyAnimator?.SetAttacking(false);
        _state.ChangeState(EnemyState.Idle);
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

    /// <summary>
    /// 攻撃ヒットタイミングのAnimationEventから中継されるハンドラ
    /// </summary>
    private void HandleAttackHit()
    {
        if (!_isAttacking) return;
        _attackHitFired = true;
        PerformAttack();
    }

    /// <summary>
    /// 攻撃終了タイミングのAnimationEventから中継されるハンドラ
    /// </summary>
    private void HandleAttackEnd()
    {
        if (!_isAttacking) return;
        _attackEndFired = true;
        Exit();
    }
}
