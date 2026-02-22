using System.Collections.Generic;
using UnityEngine;

public class EnemyAttackerSlot : IEnemyAttackerSlot
{
    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="maxAttackers"></param>
    public EnemyAttackerSlot(int maxAttackers)
    {
        this.maxAttackers = Mathf.Max(1,maxAttackers);
    }

    
    public bool TryAcquire(IEnemy enemy)
    {
        if (attackers.Contains(enemy)) return true;
        if (attackers.Count >= maxAttackers) return false;

        attackers.Add(enemy);
        enemy.OnDead += OnEnemyDead;
        return true;
    }

    public void Release(IEnemy enemy)
    {
        enemy.OnDead -= OnEnemyDead;
        attackers.Remove(enemy);
    }

    public bool IsAttacker(IEnemy enemy)
    {
        return attackers.Contains(enemy);
    }

    private readonly int maxAttackers;
    private readonly HashSet<IEnemy> attackers = new();

    private void OnEnemyDead(IEnemy enemy)
    {
        Release(enemy);
    }

}
