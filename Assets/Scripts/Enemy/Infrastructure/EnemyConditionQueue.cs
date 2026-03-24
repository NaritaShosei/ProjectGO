using System.Collections.Generic;

/// <summary>
/// Enemyが保持する状態異常のキュー
/// 同種のConditionは同時に1つまでで、新しいものが来た場合は上書きする
/// 異種のConditionは_pendingに積まれ、毎フレームの先頭で_activeに移行する
/// 同一フレームに同種のConditionが複数Enqueueされた場合は後から来たものが上書きする
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
    /// 同種がpending中の場合も上書きし、後から来たものを優先する
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

        // Dictionaryによる上書きで同一フレーム内の同種重複を防ぐ
        _pending[condition.Type] = condition;
    }

    /// <summary>
    /// Conditionを pending を経由せず即座に _active に登録し OnEnter() を呼ぶ。
    /// 死亡など Tick() が止まった後に適用が必要な場合に使用する。
    /// </summary>
    public void Apply(IEnemy enemy, IEnemyCondition condition)
    {
        if (_active.ContainsKey(condition.Type))
        {
            _active[condition.Type].OnExit(enemy);
        }
        _active[condition.Type] = condition;
        condition.OnEnter(enemy);
    }

    /// <summary>
    /// アクティブなConditionを毎フレーム進め、終了したものを解除する
    /// 先頭でpendingを処理し、OnEnterと初回Tickを同一フレームで実行する
    /// </summary>
    public void Tick(IEnemy enemy, float dt)
    {
        // 先頭でpendingを処理し、OnEnterと初回Tickを同一フレームで実行する
        if (_pending.Count > 0)
        {
            foreach (var condition in _pending.Values)
            {
                if (_active.ContainsKey(condition.Type)) continue;
                _active.Add(condition.Type, condition);
                condition.OnEnter(enemy);
            }
            _pending.Clear();
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

    /// <summary>
    /// ObjectPoolから再利用する際にすべてのConditionを強制終了してクリアする
    /// </summary>
    public void Clear(IEnemy enemy)
    {
        foreach (var condition in _active.Values)
            condition.OnExit(enemy);
        _active.Clear();
        _pending.Clear();
    }

    private readonly Dictionary<ConditionType, IEnemyCondition> _active = new();
    // Dictionaryで管理することで同一フレーム内の同種Conditionを自動的に上書きする
    private readonly Dictionary<ConditionType, IEnemyCondition> _pending = new();
}
