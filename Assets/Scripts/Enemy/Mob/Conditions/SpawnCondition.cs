
public class SpawnCondition : IEnemyCondition
{
    public ConditionType Type => ConditionType.Spawn;

    public bool BlocksAction => true;

    public bool IsFinished => _finished;

    public void OnEnter(IEnemy enemy)
    {
        if (enemy is IEnemySpawnState receiver)
        {
            receiver.SetSpawnState(true);
        }
    }

    public void OnExit(IEnemy enemy)
    {
        if (enemy is IEnemySpawnState receiver)
        {
            receiver.SetSpawnState(false);
        }
    }

    public void RequestFinish()
    {
        _finished = true;
    }

    public void Tick(IEnemy enemy, float dt) { }

    private bool _finished;
}
