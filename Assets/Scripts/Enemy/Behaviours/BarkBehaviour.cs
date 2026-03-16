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

        // 自分がすでにスロットを確保済みの場合はBarkしない
        if (_attackerSlot.IsAcquired(_enemyId)) return false;

        // 自分以外でスロットが満杯でなければBarkしない
        if (!_attackerSlot.IsFull(slotCost)) return false;

        // 確率判定：falseのときはRoamが選ばれる
        return UnityEngine.Random.value < _barkChance;
    }

    public bool CanContinue()
    {
        // タイマーが終わるまで継続
        if (_data.BarkDuration > 0f)
        {
            return _timer < _data.BarkDuration;
        }

        // BarkDuration未設定の場合はAnimationの再生終了を検知する
        if (_animator == null) return false;

        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);

        // Barkステートの再生が終了したら終了する
        if (stateInfo.IsName("Bark"))
        {
            return stateInfo.normalizedTime < 1f;
        }

        // まだBarkステートに遷移していない場合は継続する
        return true;
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

    private int _enemyId;

    // 追加
    private readonly float _barkChance;

    private readonly IEnemyAttackerSlot _attackerSlot;
    private float _timer;

    /// <summary>
    /// BarkステートのAnimationClip長を取得する
    /// BarkDurationが未設定の場合のフォールバック
    /// </summary>
}
