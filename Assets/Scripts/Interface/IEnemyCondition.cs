public interface IEnemyCondition
{
    /// <summary>
    /// ConditionTypeの保持
    /// </summary>
    ConditionType Type { get; }

    /// <summary>
    /// このConditionがActionを止められるかの判定
    /// </summary>
    bool BlocksAction { get; }

    /// <summary>
    /// 開始時に一度だけ呼ばれる
    /// </summary>
    /// <param name="enemy"></param>
    void OnEnter(IEnemy enemy);

    /// <summary>
    /// Condition継続中
    /// </summary>
    /// <param name="enemy"></param>
    /// <param name="dt"></param>
    void Tick(IEnemy enemy, float dt);

    /// <summary>
    /// 終了時に一度だけ呼ばれる
    /// </summary>
    /// <param name="enemy"></param>
    void OnExit(IEnemy enemy);

    /// <summary>
    /// 終了したかの判定
    /// </summary>
    bool IsFinished { get; }
}

public enum ConditionType : int
{
    Knockback = 1,
    Electrified = 2,
    Down = 3,
    // 拡張できるようにEnum
}
