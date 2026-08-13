using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 複数の敵で構成されるグループを管理する。
/// </summary>
public sealed class EnemyGroup
{
    public event Action<IEnemy> OnAttackerPromoted;

    public IEnemy Leader { get; private set; }

    public IReadOnlyList<IEnemy> Members =>
        _members;

    public float MemberMoveRadius { get; }

    public int MaxAttackers { get; }

    public IReadOnlyList<IEnemy> Attackers => _attackers;

    public EnemyGroupPhase Phase { get; private set; }
        = EnemyGroupPhase.Waiting;

    public EnemyGroup(float memberMoveRadius, int maxAttackers = 2)
    {
        MemberMoveRadius =
            Mathf.Max(0f, memberMoveRadius);
        MaxAttackers = Mathf.Max(1, maxAttackers);
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

    public void SetAttackers(IReadOnlyList<IEnemy> attackers)
    {
        _attackers.Clear();
        _attackerIds.Clear();

        if (attackers == null) return;

        foreach (IEnemy attacker in attackers)
        {
            if (attacker == null || attacker.IsDead) continue;

            if (_attackerIds.Add(attacker.Id))
                _attackers.Add(attacker);
        }
    }

    public bool IsAttacker(int enemyId) =>
        _attackerIds.Contains(enemyId);

    public bool TryGetFollowerIndex(
        IEnemy follower,
        out int followerIndex)
    {
        followerIndex = 0;

        if (follower == null || follower.IsDead ||
            IsAttacker(follower.Id) || _attackers.Count == 0)
            return false;

        foreach (IEnemy member in _members)
        {
            if (member == null || member.IsDead || IsAttacker(member.Id))
                continue;

            if (member.Id == follower.Id)
                return true;

            followerIndex++;
        }

        return false;
    }

    /// <summary>
    /// グループメンバーが死亡したときの処理。
    /// </summary>
    private void HandleMemberDead(IEnemy enemy)
    {
        enemy.OnDead -= HandleMemberDead;

        bool wasAttacker = _attackerIds.Contains(enemy.Id);

        _members.Remove(enemy);
        _attackerIds.Remove(enemy.Id);
        _attackers.Remove(enemy);

        if (wasAttacker && Phase == EnemyGroupPhase.Released)
        {
            PromoteReplacementAttacker();
        }

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
    /// 死亡した攻撃役の代わりに、生存中の追従役を昇格させる。
    /// </summary>
    private void PromoteReplacementAttacker()
    {
        if (_attackers.Count >= MaxAttackers)
            return;

        foreach (IEnemy member in _members)
        {
            if (member == null ||
                member.IsDead ||
                IsAttacker(member.Id))
            {
                continue;
            }

            _attackerIds.Add(member.Id);
            _attackers.Add(member);
            OnAttackerPromoted?.Invoke(member);
            return;
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

    private readonly List<IEnemy> _attackers = new();
    private readonly HashSet<int> _attackerIds = new();
}
