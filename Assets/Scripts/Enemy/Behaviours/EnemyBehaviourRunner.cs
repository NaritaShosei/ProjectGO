using System.Collections.Generic;

/// <summary>
/// Enemyの行動を管理するランナー
/// Turnは他のBehaviourと並列で毎フレーム実行される
/// </summary>
public class EnemyBehaviourRunner
{
    public EnemyBehaviourRunner(IEnemy owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// Behaviourを登録する
    /// 登録後は優先度順に並べ替える
    /// </summary>
    public void Register(IEnemyBehaviour behaviour)
    {
        _behaviours.Add(behaviour);
        // 優先度の高い順に並べ替える
        _behaviours.Sort((a, b) => b.Priority.CompareTo(a.Priority));
    }

    /// <summary>
    /// Turnのみ並列スロットに登録する
    /// </summary>
    public void RegisterTurn(IEnemyBehaviour turnBehaviour)
    {
        _turnBehaviour = turnBehaviour;
    }

    /// <summary>
    /// 毎フレーム呼ぶ
    /// Turnは常に並列実行、それ以外は排他制御
    /// </summary>
    public void Tick(float deltaTime)
    {
        // Conditionによって行動がブロックされている場合はTurnも止める
        if (_owner.ConditionController?.BlocksAction == true) return;

        // Turnは常に並列実行
        _turnBehaviour?.Tick(deltaTime);

        // 強制Behaviourが設定されている場合はそちらを優先
        if (_forced != null)
        {
            _forced.Tick(deltaTime);
            return;
        }

        // 現在Behaviourの継続判定
        if (_current != null && _current.CanContinue())
        {
            _current.Tick(deltaTime);
            return;
        }

        // 再選択
        SelectBehaviour();
        _current?.Tick(deltaTime);
    }

    /// <summary>
    /// 指定したBehaviourを強制的に実行する
    /// 主にAttack割り込みで使用する
    /// </summary>
    public void ForceBehaviour(IEnemyBehaviour behaviour)
    {
        _current?.OnExit();
        _forced = behaviour;
        _forced.OnEnter();
    }

    /// <summary>
    /// 強制Behaviourを終了する
    /// Animationイベントなど外部からの終了通知で呼ぶ
    /// </summary>
    public void OnActionFinished()
    {
        _forced?.OnExit();
        _forced = null;
        _current = null;
    }

    /// <summary>
    /// Conditionによる割り込みで現在のActionを強制終了する
    /// </summary>
    public void ForceExitAction()
    {
        _forced?.OnExit();
        _forced = null;
        _current?.OnExit();
        _current = null;
    }

    private readonly IEnemy _owner;

    private readonly List<IEnemyBehaviour> _behaviours
        = new List<IEnemyBehaviour>(8);

    // 通常の排他制御Behaviour
    private IEnemyBehaviour _current;

    // 強制実行Behaviour（Attack割り込みなど）
    private IEnemyBehaviour _forced;

    // Turn専用の並列スロット
    private IEnemyBehaviour _turnBehaviour;

    private void SelectBehaviour()
    {
        // 実行可能なBehaviourのうち優先度が最も高いものを選択する
        for (int i = 0; i < _behaviours.Count; i++)
        {
            var next = _behaviours[i];
            if (!next.CanEnter()) continue;

            SwitchTo(next);
            return;
        }

        _current?.OnExit();
        _current = null;
    }

    private void SwitchTo(IEnemyBehaviour next)
    {
        if (_current == next) return;

        _current?.OnExit();
        _current = next;
        _current.OnEnter();
    }
}

/// <summary>
/// Behaviourの優先度
/// 数値が大きいほど優先度が高い
/// </summary>
public enum EnemyBehaviourPriority : int
{
    None = 0,
    Roam = 1,
    Bark = 2,
    Move = 3,
    Attack = 4
}
