using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 複数の敵で構成されるグループを管理する。
/// </summary>
public sealed class EnemyGroup
{
    public IEnemy Leader { get; private set; }

    public IReadOnlyList<IEnemy> Members =>
        _members;

    public float MemberMoveRadius { get; }

    public EnemyGroupPhase Phase { get; private set; }
        = EnemyGroupPhase.Waiting;

    public EnemyGroup(float memberMoveRadius)
    {
        MemberMoveRadius =
            Mathf.Max(0f, memberMoveRadius);
    }

    /// <summary>
    /// グループにメンバーを追加する。
    /// </summary>
    public void AddMember(
        IEnemy enemy,
        IEnemyGroupMember groupMember,
        bool isLeader)
    {
        if (enemy == null || groupMember == null)
            return;

        if (_members.Contains(enemy))
            return;

        _members.Add(enemy);
        _groupMembers.Add(enemy, groupMember);

        groupMember.AssignGroup(
            this,
            isLeader);

        enemy.OnDead += HandleMemberDead;

        if (isLeader)
        {
            SetLeader(enemy);
        }
    }

    /// <summary>
    /// グループを解放する。
    /// </summary>
    public void ReleaseFormation()
    {
        Phase = EnemyGroupPhase.Released;
    }

    /// <summary>
    /// グループメンバーが死亡したときの処理。
    /// </summary>
    private void HandleMemberDead(IEnemy enemy)
    {
        enemy.OnDead -= HandleMemberDead;

        _members.Remove(enemy);

        if (_groupMembers.TryGetValue(
                enemy,
                out IEnemyGroupMember groupMember))
        {
            groupMember.ClearGroup();
            _groupMembers.Remove(enemy);
        }

        if (Leader != enemy)
            return;

        Leader = null;

        if (Phase == EnemyGroupPhase.Waiting &&
            _members.Count > 0)
        {
            SetLeader(_members[0]);
        }
    }

    /// <summary>
    /// グループのリーダーを設定する。
    /// </summary>
    private void SetLeader(IEnemy enemy)
    {
        if (Leader != null &&
            _groupMembers.TryGetValue(
                Leader,
                out IEnemyGroupMember oldLeader))
        {
            oldLeader.SetGroupLeader(false);
        }

        Leader = enemy;

        if (_groupMembers.TryGetValue(
                Leader,
                out IEnemyGroupMember newLeader))
        {
            newLeader.SetGroupLeader(true);
        }
    }

    private readonly List<IEnemy> _members = new();

    private readonly Dictionary<IEnemy, IEnemyGroupMember>
        _groupMembers = new();
}
