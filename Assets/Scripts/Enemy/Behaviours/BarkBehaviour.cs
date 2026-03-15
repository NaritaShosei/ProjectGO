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
        EnemyStateContext state
    )
    {
        _self = owner.transform;

        // 追加
        _enemyId = owner.GetInstanceID();

        _player = player;
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
        return _timer < _data.BarkDuration;
    }

    public void OnEnter()
    {
        _timer = 0f;
        _state.ChangeState(EnemyState.Bark);
    }

    public void OnExit()
    {
        _state.ChangeState(EnemyState.Idle);
    }

    public void Tick(float deltaTime)
    {
        _timer += deltaTime;
    }

    private Transform _self;
    private Transform _player;
    private EnemyData _data;
    private EnemyContext _context;
    private EnemyStateContext _state;

    private int _enemyId;


    // 追加
    private readonly float _barkChance;

    private readonly IEnemyAttackerSlot _attackerSlot;
    private float _timer;
}
