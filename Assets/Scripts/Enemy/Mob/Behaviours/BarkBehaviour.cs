using UnityEngine;

/// <summary>
/// 攻撃距離内にいるがスロットが埋まっているときに威嚇するBehaviour
/// BarkDurationの時間が経過したら終了する
/// </summary>
public class BarkBehaviour : IEnemyBehaviour
{
    public int Priority { get => (int)EnemyBehaviourPriority.Bark; }

    /// <summary>
    /// DistanceProfile・AttackerSlot はBarkBehaviour固有の依存のためコンストラクタで受け取る
    /// </summary>
    public BarkBehaviour(DistanceProfile profile, EnemyServices services, float barkChance)
    {
        _profile = profile;
        _attackerSlot = services.AttackerSlot;
        _barkChance = barkChance;
    }

    public void Init(BehaviourInitContext ctx)
    {
        _self = ctx.Owner.Self;
        _enemyAnimator = ctx.EnemyAnimator;
        _enemyId = ctx.Owner.Id;
        _player = ctx.Player;
        _data = ctx.Data;
        _context = ctx.RuntimeContext;
        _state = ctx.StateContext;

        // OnBarkEndイベントを購読してBark終了を検知する
        if (_enemyAnimator != null)
        {
            _enemyAnimator.OnBarkEnd += HandleBarkEnd;
        }
    }

    public bool CanEnter()
    {
        if (_attackerSlot == null) return false;
        if (_player == null) return false;

        // リポジション必要時はBarkしない（Roamに委ねる）
        float dist = Vector3.Distance(_self.position, _player.position);
        if (dist > _profile.MaxRoamDistance) return false;
        if (!_attackerSlot.IsAcquired(_enemyId) && dist < _profile.MinNonAttackerDistance) return false;

        // クールダウン中またはスロット未確保のときにBarkする
        // 攻撃権を持ちクールダウンも終わっている場合はAttackが優先されるためBarkしない
        bool isOnCooldown = _context.AttackCooldownRemaining > 0f;
        bool hasNoSlot = !_attackerSlot.IsAcquired(_enemyId);
        if (!isOnCooldown && !hasNoSlot) return false;

        // 確率判定：falseのときはRoamが選ばれる
        return UnityEngine.Random.value < _barkChance;
    }

    public bool CanContinue()
    {
        // BarkDurationが設定されている場合はタイマーで終了判定する
        if (_data.BarkDuration > 0f)
        {
            // BarkDuration設定時はtimerのみで終了判定する
            // AnimationEventのタイミングに依存しないため安定した終了が保証される
            return _timer < _data.BarkDuration;
        }

        // BarkDuration未設定時はAnimationEventで終了を検知する
        return !_barkEnded;
    }

    public void OnEnter()
    {
        _timer = 0f;
        _barkEnded = false;
        _state.ChangeState(EnemyState.Bark);
        _enemyAnimator?.SetBarking(true);
    }

    public void OnExit()
    {
        _state.ChangeState(EnemyState.Idle);
        _enemyAnimator?.SetBarking(false);
    }

    public void Tick(float deltaTime)
    {
        _timer += deltaTime;
    }

    /// <summary>
    /// イベント購読を解除する
    /// </summary>
    public void Dispose()
    {
        if (_enemyAnimator != null)
        {
            _enemyAnimator.OnBarkEnd -= HandleBarkEnd;
        }
    }

    private Transform _self;
    private Transform _player;
    private EnemyData _data;
    private EnemyRuntimeContext _context;
    private EnemyStateContext _state;
    private IEnemyAnimator _enemyAnimator;

    private int _enemyId;
    private bool _barkEnded;

    private readonly DistanceProfile _profile;
    private readonly float _barkChance;

    private readonly IEnemyAttackerSlot _attackerSlot;
    private float _timer;

    /// <summary>
    /// BarkアニメーションのAnimationEventから中継されるハンドラ
    /// </summary>
    private void HandleBarkEnd()
    {
        _barkEnded = true;
    }
}
