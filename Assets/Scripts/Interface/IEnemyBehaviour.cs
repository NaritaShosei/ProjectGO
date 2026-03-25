using UnityEngine;

/// <summary>
/// Enemyの行動単位を表すインターフェース
/// BehaviourRunnerに登録してPriorityに従って実行される
/// </summary>
public interface IEnemyBehaviour
{
    /// <summary>行動の優先度。値が大きいほど優先される</summary>
    int Priority { get; }

    /// <summary>この行動を開始できるかどうか</summary>
    bool CanEnter();

    /// <summary>この行動を継続できるかどうか</summary>
    bool CanContinue();

    /// <summary>行動開始時に一度だけ呼ばれる</summary>
    void OnEnter();

    /// <summary>毎フレーム呼ばれる</summary>
    void Tick(float deltaTime);

    /// <summary>行動終了時に一度だけ呼ばれる</summary>
    void OnExit();

    /// <summary>
    /// 行動の初期化。BehaviourRunnerへの登録後、Registerの直後に呼ぶこと
    /// </summary>
    void Init(BehaviourInitContext ctx);
}

/// <summary>
/// IEnemyBehaviour.Init に渡す初期化コンテキスト
/// </summary>
public readonly struct BehaviourInitContext
{
    public readonly IEnemy Owner;
    public readonly EnemyData Data;
    public readonly Transform Player;
    public readonly EnemyRuntimeContext RuntimeContext;
    public readonly IEnemyAnimator EnemyAnimator;
    public readonly EnemyStateContext StateContext;

    public BehaviourInitContext(
        IEnemy owner,
        EnemyData data,
        Transform player,
        EnemyRuntimeContext runtimeContext,
        IEnemyAnimator enemyAnimator,
        EnemyStateContext stateContext)
    {
        Owner = owner;
        Data = data;
        Player = player;
        RuntimeContext = runtimeContext;
        EnemyAnimator = enemyAnimator;
        StateContext = stateContext;
    }
}
