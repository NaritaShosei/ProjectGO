using System.Collections.Generic;

/// <summary>
/// Enemyが保持する状態異常のキュー
/// 同種のConditionは同時に1つまでで、新しいものが来た場合は上書きする
/// 異種のConditionは_pendingに積まれ、毎フレームの先頭で_activeに移行する
/// </summary>
public sealed class EnemyConditionQueue
{
    /// <summary>
    /// アクティブなConditionに1つでもBlocksAction=trueのものがあればtrueを返す
    /// </summary>
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

    /// <summary>
    /// 指定したConditionTypeが発動中かを返す
    /// </summary>
    public bool Has(ConditionType type)
        => _active.ContainsKey(type);

    /// <summary>
    /// Conditionをキューに追加する
    /// 同種がすでにアクティブな場合は上書きし、即座にOnEnterを呼ぶ
    /// </summary>
    public void Enqueue(IEnemy enemy, IEnemyCondition condition)
    {
        if (Has(condition.Type))
        {
            _active[condition.Type].OnExit(enemy);
            _active[condition.Type] = condition;
            condition.OnEnter(enemy);
            return;
        }

        _pending.Enqueue(condition);
    }

    /// <summary>
    /// アクティブなConditionを毎フレーム進め、終了したものを解除する
    /// その後_pendingのConditionを_activeに移行する
    /// </summary>
    public void Tick(IEnemy enemy, float dt)
    {
        // 先頭でpendingを処理し、OnEnterと初回Tickを同一フレームで実行する
        while (_pending.Count > 0)
        {
            var next = _pending.Dequeue();
            if (_active.ContainsKey(next.Type)) continue;

            _active.Add(next.Type, next);
            next.OnEnter(enemy);
        }

        var finished = ListPool<IEnemyCondition>.Get();

        foreach (var condition in _active.Values)
        {
            condition.Tick(enemy, dt);
            if (condition.IsFinished)
                finished.Add(condition);
        }

        for (int i = 0; i < finished.Count; i++)
        {
            var condition = finished[i];
            condition.OnExit(enemy);
            _active.Remove(condition.Type);
        }

        ListPool<IEnemyCondition>.Release(finished);
    }

    private readonly Dictionary<ConditionType, IEnemyCondition> _active = new();
    private readonly Queue<IEnemyCondition> _pending = new();
}
