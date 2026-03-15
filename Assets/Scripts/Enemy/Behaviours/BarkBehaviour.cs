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
    public BarkBehaviour(IEnemyAttackerSlot attackerSlot, float barkChance)
    {
        _attackerSlot = attackerSlot;
        _barkChance = barkChance;
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
        _enemyAnimator = enemyAnimator;
        _enemyId = owner.GetInstanceID();
        _player = player;
        _animator = animator;
        _data = data;
        _context = context;
        _state = state;
    }

    public bool CanEnter()
    {
        if (_attackerSlot == null) return false;

        int slotCost = _data.AttackPattern != null
            ? _data.AttackPattern.SlotCost
            : 1;

        // スロットが満杯でなければ発動しない
        if (!_attackerSlot.IsFull(slotCost)) return false;

        // 確率判定：falseのときはRoamが選ばれる
        return UnityEngine.Random.value < _barkChance;
    }

    public bool CanContinue()
    {
        // スロットが確保されたら即座に終了してMoveへ切り替わる
        if (_attackerSlot != null && _attackerSlot.IsAcquired(_enemyId)) return false;

        // タイマーが終わるまで継続
        float duration = _data.BarkDuration > 0f
            ? _data.BarkDuration
            : GetAnimationLength();

        return _timer < duration;
    }

    public void OnEnter()
    {
        _timer = 0f;
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

    private Transform _self;
    private Transform _player;
    private EnemyData _data;
    private EnemyContext _context;
    private Animator _animator;
    private EnemyStateContext _state;
    private EnemyAnimator _enemyAnimator;

    // BarkステートのAnimatorハッシュ
    private static readonly int _barkStateHash = Animator.StringToHash("Bark");

    private int _enemyId;

    // 追加
    private readonly float _barkChance;

    private readonly IEnemyAttackerSlot _attackerSlot;
    private float _timer;

    /// <summary>
    /// BarkステートのAnimationClip長を取得する
    /// BarkDurationが未設定の場合のフォールバック
    /// </summary>
    private float GetAnimationLength()
    {
        if (_animator == null) return 0f;

        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);

        if (stateInfo.IsName("Bark"))
        {
            return stateInfo.length;
        }

        // Barkステートに遷移前の場合は次フレームまで待つ
        return float.MaxValue;
    }
}
