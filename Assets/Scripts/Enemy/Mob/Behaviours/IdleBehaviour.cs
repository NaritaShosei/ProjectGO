/// <summary>
/// Roam・Barkが選択されなかったときのフォールバックとして一定時間静止するBehaviour
/// SelectBehaviourのskip-previousルールにより Roam → Idle → Roam のサイクルが成立する
/// </summary>
public class IdleBehaviour : IEnemyBehaviour
{
    public int Priority { get => (int)EnemyBehaviourPriority.Idle; }

    public void Init(BehaviourInitContext ctx)
    {
        if (ctx.Data == null)
            UnityEngine.Debug.LogWarning("[IdleBehaviour] ctx.Data が null です。IdleDuration は 1 秒にフォールバックします。");
        _data = ctx.Data;
        _enemyAnimator = ctx.EnemyAnimator;
        _state = ctx.StateContext;
    }

    public bool CanEnter()
    {
        if (_state == null) return false;
        return _state.CanMove();
    }

    public bool CanContinue() => _timer > 0f;

    public void OnEnter()
    {
        _state.ChangeState(EnemyState.Idle);
        _enemyAnimator?.SetSpeed(0f);
        _timer = _data != null ? _data.IdleDuration : 1f;
    }

    public void Tick(float deltaTime)
    {
        _timer -= deltaTime;
        _enemyAnimator?.SetSpeed(0f);
    }

    public void OnExit()
    {
        // 次のBehaviourのOnEnter()でStateが上書きされるため、ここでは何もしない
    }

    private EnemyData _data;
    private IEnemyAnimator _enemyAnimator;
    private EnemyStateContext _state;
    private float _timer;
}
