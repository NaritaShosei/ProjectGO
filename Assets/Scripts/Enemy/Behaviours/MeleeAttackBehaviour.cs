using UnityEngine;

/// <summary>
/// 近接攻撃Behaviour
/// AttackerSlotでスロットを確保できた場合のみ攻撃する
/// AttackPatternのDuration・BaseDamage・Cooldown・MaxHitCount・HitIntervalを参照する
/// WindUpはAnimationEventのOnAttackHit発火タイミングでアニメーション側が制御する
/// </summary>
public class MeleeAttackBehaviour : IEnemyBehaviour
{
    public int Priority { get => (int)EnemyBehaviourPriority.Attack; }

    /// <summary>
    /// DistanceProfile・AttackerSlot・AnimatorはMeleeAttackBehaviour固有の依存のためコンストラクタで受け取る
    /// </summary>
    public MeleeAttackBehaviour(DistanceProfile profile, EnemyServices services, Animator animator)
    {
        _profile = profile;
        _attackerSlot = services.AttackerSlot;
        _animator = animator;
    }

    public void Init(BehaviourInitContext ctx)
    {
        _self = ctx.Owner.GetTargetCenter();
        _enemyId = ctx.Owner.Id;
        _player = ctx.Player;
        _data = ctx.Data;
        _context = ctx.RuntimeContext;
        _enemyAnimator = ctx.EnemyAnimator;
        _state = ctx.StateContext;

        // AttackHit / AttackEndイベントを購読してアニメーションと同期する
        if (_enemyAnimator != null)
        {
            _enemyAnimator.OnAttackHit += HandleAttackHit;
            _enemyAnimator.OnAttackEnd += HandleAttackEnd;
        }
    }

    public bool CanEnter()
    {
        if (_player == null) return false;
        if (_attackerSlot == null) return false;
        if (_isAttacking) return false;

        // スポーン時に確保済みのスロットを持っているかチェック
        if (!_attackerSlot.IsAcquired(_enemyId)) return false;

        // クールダウン判定（0以下で攻撃可能）
        if (_context.AttackCooldownRemaining > 0f) return false;

        // 射程チェック
        _context.DistanceToPlayer = Vector3.Distance(_self.position, _player.position);
        return _context.DistanceToPlayer <= _data.AttackRange;
    }

    public bool CanContinue()
    {
        if (!_isAttacking) return false;

        // プレイヤーが最大射程外に逃げた場合は攻撃を中断する
        if (_profile != null && _player != null)
        {
            float dist = Vector3.Distance(_self.position, _player.position);
            if (dist > _profile.MaxAttackDistance) return false;
        }

        return true;
    }

    public void OnEnter()
    {
        _isAttacking = true;
        _timer = 0f;
        _hitCount = 0;
        _nextHitTime = float.MaxValue;
        _attackEndFired = false;
        _state.ChangeState(EnemyState.Attack);
        _enemyAnimator?.SetAttacking(true);
    }

    public void Tick(float deltaTime)
    {
        if (!_isAttacking) return;

        _timer += deltaTime;

        // 多段ヒット：前回ヒットからHitInterval経過後に追加ヒットを処理する
        int maxHitCount = _data.AttackPattern != null ? _data.AttackPattern.MaxHitCount : 1;
        if (_hitCount > 0 && _hitCount < maxHitCount && _timer >= _nextHitTime)
        {
            PerformHit();
        }

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

    /// <summary>
    /// 死亡時にEnemyから明示的に呼び出し、AttackerSlotを解放する
    /// </summary>
    public void ReleaseSlot()
    {
        if (_attackerSlot == null) return;

        int slotCost = _data.AttackPattern != null
            ? _data.AttackPattern.SlotCost
            : 1;

        _attackerSlot.Release(_enemyId, slotCost);
    }

    private Transform _self;
    private Transform _player;
    private EnemyData _data;
    private EnemyRuntimeContext _context;
    private EnemyStateContext _state;
    private IEnemyAnimator _enemyAnimator;
    private Animator _animator;

    private readonly DistanceProfile _profile;
    private readonly IEnemyAttackerSlot _attackerSlot;

    private int _enemyId;
    private float _timer;
    private float _nextHitTime;
    private bool _isAttacking;
    private int _hitCount;
    private bool _attackEndFired;

    // AnimationEventが来ない場合の攻撃強制終了タイムアウト（秒）
    private const float _attackFallbackTimeout = 5f;

    /// <summary>
    /// 実際の攻撃判定とダメージ適用を行う
    /// BaseDamageはAttackPatternから取得し、未設定の場合はEnemyDataにフォールバックする
    /// </summary>
    private void PerformHit()
    {
        var pattern = _data.AttackPattern;
        float damage = (pattern != null && pattern.BaseDamage > 0)
            ? pattern.BaseDamage
            : _data.AttackDamage;

        string patternName = pattern != null ? pattern.PatternName : "default";

#if UNITY_EDITOR
        Vector3 center = _self.position + _self.forward * _data.AttackRange;
        Debug.Log($"[Attack:{patternName}] hit={_hitCount + 1}, center={center}, radius={_data.AttackRadius}");
        Collider[] debugHits = Physics.OverlapSphere(center, _data.AttackRadius);
        foreach (var h in debugHits)
        {
            Debug.Log($"[Attack:{patternName}] target={h.gameObject.name}, hasIPlayer={h.TryGetComponent<IPlayer>(out _)}");
        }
#endif

        Collider[] hits = Physics.OverlapSphere(
            _self.position + _self.forward * _data.AttackRange,
            _data.AttackRadius
        );

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out IPlayer player))
            {
                player.TakeDamage(damage);
            }
        }

        _hitCount++;

        // 次のヒット時刻を設定する
        float hitInterval = pattern != null ? pattern.HitInterval : 0f;
        _nextHitTime = _timer + hitInterval;
    }

    private void Exit()
    {
        if (!_isAttacking) return;
        _isAttacking = false;

        // 攻撃後クールダウンをセット（AttackPatternが設定されていれば優先する）
        var pattern = _data.AttackPattern;
        float cooldown = (pattern != null && pattern.Cooldown > 0f)
            ? pattern.Cooldown
            : _data.AttackCooldown;
        _context.AttackCooldownRemaining = cooldown;

        _enemyAnimator?.SetAttacking(false);
        _state.ChangeState(EnemyState.Idle);
    }

    /// <summary>
    /// AttackステートのAnimationClip長を取得する
    /// AttackPattern.Durationが未設定の場合のフォールバック
    /// </summary>
    private float GetAnimationLength()
    {
        // Animatorがnullのときはフォールバックタイムアウトを返して攻撃が即終了しないようにする
        if (_animator == null) return _attackFallbackTimeout;

        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);

        if (stateInfo.IsName("Attack"))
        {
            return stateInfo.length;
        }

        // Attackステートに遷移前、またはClip長取得不能の場合はフォールバックタイムアウトを返す
        return _attackFallbackTimeout;
    }

    /// <summary>
    /// 攻撃ヒットタイミングのAnimationEventから中継されるハンドラ
    /// WindUpはアニメーション側でOnAttackHitの発火タイミングとして制御される
    /// </summary>
    private void HandleAttackHit()
    {
        if (!_isAttacking) return;

        // 初回ヒットのみAnimationEventで処理し、以降はTick内でHitIntervalに従って処理する
        if (_hitCount > 0) return;

        PerformHit();
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
