using System.Collections.Generic;

/// <summary>
/// Enemyが状態異常を保持するためのQueue
/// 例えばKnockbackとElectrifiedを同時に受けた時など
/// </summary>
public sealed class EnemyConditionQueue
{
    public bool HasActive => _active.Count > 0;

    // すでに同種のConditionを持っているか
    public bool Has(ConditionType type)
        => _active.ContainsKey(type);

    public void Enqueue(IEnemy enemy, IEnemyCondition condition)
    {
        // 同種は最大1
        if (Has(condition.Type))
        {
            _active[condition.Type].OnExit(enemy);
            // 上書き（Knockback等）
            _active[condition.Type] = condition;
            condition.OnEnter(enemy);
            return;
        }

        _pending.Enqueue(condition);
    }

    // 適用中のConditionを順番に獲得
    public IEnumerable<IEnemyCondition> ActiveConditions
        => _active.Values;

    // EnemyConditionControllerから呼び出される予定
    public void Tick(IEnemy enemy, float dt)
    {
        // 終了したものを格納する用のPoolをもらってくる
        var finished = EnemyListPool<IEnemyCondition>.Get();
        // Activeの処理
        foreach (var condition in _active.Values)
        {
            condition.Tick(enemy, dt);
            if (condition.IsFinished)
                finished.Add(condition);
        }
        // 終了したものを解除
        for (int i = 0; i < finished.Count; i++)
        {
            var condition = finished[i];
            condition.OnExit(enemy);
            _active.Remove(condition.Type);
        }
        // Poolを返す。
        EnemyListPool<IEnemyCondition>.Release(finished);

        // Pending → Active
        while (_pending.Count > 0)
        {
            // _pendingから取り出して
            var next = _pending.Dequeue();
            // _activeに追加で初期メソッドを実行
            _active.Add(next.Type, next);
            next.OnEnter(enemy);
        }
    }

    // conditionに一つでもActionを止めるものがあればtrue（回転含めすべて止める。）
    public bool BlocksAction
    {
        get
        {
            foreach (var condition in _active.Values)
                if (condition.BlocksAction)
                    return true;
            return false;
        }
    }

    private readonly Dictionary<ConditionType, IEnemyCondition> _active = new();

    private readonly Queue<IEnemyCondition> _pending = new();
}
