/// <summary>
/// グループが前衛へ出られるまで待機する。
/// 前衛への昇格判定はリーダーだけが行う。
/// </summary>
public sealed class GroupPromotionBehaviour
    : IEnemyBehaviour
{
    public int Priority =>
        (int)EnemyBehaviourPriority.GroupWaiting;

    public GroupPromotionBehaviour(
        IEnemyGroupMember groupMember,
        IEnemyFormationSystem formationSystem)
    {
        _groupMember = groupMember;
        _formationSystem = formationSystem;
    }

    public void Init(
        BehaviourInitContext ctx)
    {
        _state = ctx.StateContext;
    }

    public bool CanEnter()
    {
        return IsWaiting();
    }

    public bool CanContinue()
    {
        return IsWaiting();
    }

    public void OnEnter()
    {
        _state?.ChangeState(
            EnemyState.Idle);
    }

    public void Tick(float deltaTime)
    {
        if (!IsWaiting())
            return;

        // 昇格判定をするのはリーダーだけ
        if (!_groupMember.IsGroupLeader)
            return;

        EnemyGroup group =
            _groupMember.Group;

        if (_formationSystem.TryPromoteGroup(
                group))
        {
            UnityEngine.Debug.Log(
                $"ストーンリンググループを前衛化: " +
                $"{group.Members.Count}体");
        }
    }

    public void OnExit()
    {
    }

    private bool IsWaiting()
    {
        EnemyGroup group =
            _groupMember.Group;

        return group != null &&
               group.Phase ==
               EnemyGroupPhase.Waiting;
    }

    private readonly IEnemyGroupMember
        _groupMember;

    private readonly IEnemyFormationSystem
        _formationSystem;

    private EnemyStateContext _state;
}
