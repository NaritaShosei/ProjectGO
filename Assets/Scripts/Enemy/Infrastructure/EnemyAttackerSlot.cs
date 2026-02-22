using System.Collections.Generic;
using System;

public class EnemyAttackerSlot : IEnemyAttackerSlot
{
    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="maxAttackers"></param>
    public EnemyAttackerSlot(int maxAttackers)
    {
        this._maxAttackers = Math.Max(1,maxAttackers);
    }

    
    public bool TryAcquire(IEnemy enemy)
    {
        if (_attackers.Contains(enemy)) return true;
        if (_attackers.Count >= _maxAttackers) return false;

        _attackers.Add(enemy);
        enemy.OnDead += OnEnemyDead;
        return true;
    }

    public void Release(IEnemy enemy)
    {
        enemy.OnDead -= OnEnemyDead;
        _attackers.Remove(enemy);
    }

    public bool IsAttacker(IEnemy enemy)
    {
        return _attackers.Contains(enemy);
    }

    private readonly int _maxAttackers;
    private readonly HashSet<IEnemy> _attackers = new();

    private void OnEnemyDead(IEnemy enemy)
    {
        Release(enemy);
    }

}
