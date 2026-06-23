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
        _turnBehaviour?.OnEnter();
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
            var forced = _forced;
            forced.Tick(deltaTime);

            // Tick中にOnActionFinished等で差し替え/解除された場合はここで抜ける
            if (!ReferenceEquals(_forced, forced))
            {
                return;
            }

            if (!forced.CanContinue())
            {
                forced.OnExit();
                _forced = null;
                _current = null;
            }
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
        _forced?.OnExit();
        _current?.OnExit();
        _current = null;
        _forced = behaviour;
        _forced?.OnEnter();
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

    /// <summary>
    /// ObjectPoolから再利用する際に実行状態をリセットする。
    /// 登録済みのBehaviourリストは保持したまま、実行中の状態だけをクリアする。
    /// </summary>
    public void Reset()
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
        // 現在のBehaviourを先に終了させてからCanEnterを評価する
        // これにより同じBehaviourが即再選択されることを防ぐ
        IEnemyBehaviour previous = _current;
        _current = null;

        for (int i = 0; i < _behaviours.Count; i++)
        {
            var next = _behaviours[i];
            if (ReferenceEquals(next, previous)) continue;
            if (!next.CanEnter()) continue;
            previous?.OnExit();
            SwitchTo(next);
            return;
        }
        // 選択できるBehaviourがない場合はpreviousのOnExitだけ呼んで終了する
        previous?.OnExit();
    }

    private void SwitchTo(IEnemyBehaviour next)
    {
        // 同一Behaviourへの切り替えは無視する
        if (_current == next) return;
        // OnExitは呼び出し元（SelectBehaviour / Force系メソッド）で呼び済みの前提
        // SwitchToは切り替えとOnEnterのみを担う
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
    Idle = 1,
    Roam = 2,
    Bark = 3,
    Approach = 4,
    Attack = 5
}
