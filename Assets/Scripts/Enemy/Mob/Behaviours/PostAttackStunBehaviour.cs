/// <summary>
/// 攻撃後、一定時間その場で停止する。
/// </summary>
public sealed class PostAttackStunBehaviour : IEnemyBehaviour
{
    public int Priority => (int)EnemyBehaviourPriority.Attack;

    public PostAttackStunBehaviour(float duration)
    {
        _duration = duration;
    }

    public void Init(BehaviourInitContext ctx)
    {
        _enemyAnimator = ctx.EnemyAnimator;
        _state = ctx.StateContext;
    }

    public bool CanEnter()
    {
        return false;
    }

    public bool CanContinue()
    {
        return _remainingTime > 0f;
    }

    public void OnEnter()
    {
        _remainingTime = _duration;

        _state.ChangeState(EnemyState.Idle);
        _enemyAnimator?.SetSpeed(0f);
    }

    public void Tick(float deltaTime)
    {
        _remainingTime -= deltaTime;
        _enemyAnimator?.SetSpeed(0f);
    }

    public void OnExit()
    {
    }

    private readonly float _duration;

    private float _remainingTime;
    private IEnemyAnimator _enemyAnimator;
    private EnemyStateContext _state;
}
