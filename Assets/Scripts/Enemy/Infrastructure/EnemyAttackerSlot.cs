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
        this.maxAttackers = maxAttackers;
    }

    
    public bool TryAcquire(IEnemy enemy)
    {
        if (attackers.Contains(enemy)) return true;
        if (attackers.Count >= maxAttackers) return false;

        attackers.Add(enemy);
        return true;
    }

    public void Release(IEnemy enemy)
    {
        attackers.Remove(enemy);
    }

    public bool IsAttacker(IEnemy enemy)
    {
        return attackers.Contains(enemy);
    }

    private readonly int maxAttackers;
    private readonly HashSet<IEnemy> attackers = new();
}
