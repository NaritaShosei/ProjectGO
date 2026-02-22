using System.Collections.Generic;

/// <summary>
/// Enemyが状態異常を保持するためのQueue
/// 例えばKnockbackとElectrifiedを同時に受けた時など
/// </summary>
public sealed class EnemyConditionQueue
{
    private readonly Dictionary<ConditionType, IEnemyCondition> _active
        = new();

    private readonly Queue<IEnemyCondition> _pending = new();

    public bool HasActive => _active.Count > 0;

    public bool Has(ConditionType type)
        => _active.ContainsKey(type);

    public void Enqueue(IEnemyCondition condition)
    {
        // 同種は最大1
        if (_active.ContainsKey(condition.Type))
        {
            // 上書き（Knockback等）
            _active[condition.Type] = condition;
            return;
        }

        _pending.Enqueue(condition);
    }

    public IEnumerable<IEnemyCondition> ActiveConditions
        => _active.Values;

    public void Tick(IEnemy enemy, float dt)
    {
        // Active
        var finished = EnemyListPool<IEnemyCondition>.Get();
        foreach (var c in _active.Values)
        {
            c.Tick(enemy, dt);
            if (c.IsFinished)
                finished.Add(c);
        }

        for (int i = 0; i < finished.Count; i++)
        {
            var c = finished[i];
            c.OnExit(enemy);
            _active.Remove(c.Type);
        }
        EnemyListPool<IEnemyCondition>.Release(finished);

        // Pending → Active
        while (_pending.Count > 0)
        {
            var next = _pending.Dequeue();
            if (_active.ContainsKey(next.Type)) continue;

            _active.Add(next.Type, next);
            next.OnEnter(enemy);
        }
    }

    public bool BlocksAction
    {
        get
        {
            foreach (var c in _active.Values)
                if (c.BlocksAction)
                    return true;
            return false;
        }
    }
}
