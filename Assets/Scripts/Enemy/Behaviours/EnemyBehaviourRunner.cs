using System.Collections.Generic;

/// <summary>
/// TODO: 作り変えたけど、Turnも同時に実行できるようにしなければ。。
/// </summary>
public class EnemyBehaviourRunner
{
    public EnemyBehaviourRunner(IEnemy owner)
    {
        _owner = owner;
    }

    public void Register(IEnemyBehaviour behaviour)
    {
        _behaviours.Add(behaviour);
        // Actionごとの優先度を比較、並べ替える
        _behaviours.Sort((behaviour1, behaviour2) => behaviour2.Priority.CompareTo(behaviour1.Priority));
    }


    // TODO: どれか一つのActionしか実施できないようになっているので
    // あとでTurnの処理を今後追加する。
    public void Tick(float deltaTime)
    {
        // 強制挙動（Attackなど）
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


    // ===== Attack / Condition 割り込み =====

    public void ForceBehaviour(IEnemyBehaviour behaviour)
    {
        _current?.OnExit();
        _forced = behaviour;
        _forced.OnEnter();
    }

    // Knockback時など強制発動
    public void ForceExitAttack()
    {
        if (_forced is MeleeAttackBehaviour)
        {
            _forced.OnExit();
            _forced = null;
        }
    }

    public void OnActionFinished()
    {
        _forced?.OnExit();
        _forced = null;
        _current?.OnExit();
        _current = null;
    }


    private void SelectBehaviour()
    {
        // 実行可能なBehaviourのもののうち1番目を選択する。
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

    // どこかで使うかもなので一応保持
    private readonly IEnemy _owner;

    private readonly List<IEnemyBehaviour> _behaviours
        = new List<IEnemyBehaviour>(8);

    private IEnemyBehaviour _current;
    private IEnemyBehaviour _forced;

}

/// <summary>
/// 1とかじゃなくもっと大きい数字のほうがわかりやすいかもだけど、そもそもTurnを入れる段階で削るかもしれない。
/// </summary>
public enum EnemyBehaviourPriority : int
{
    None = 0,
    Roam = 1,
    Bark = 2,
    Move = 3,
    Attack = 4
}

