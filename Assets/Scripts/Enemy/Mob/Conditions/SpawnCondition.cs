using UnityEngine;

public class SpawnCondition : IEnemyCondition
{
    public ConditionType Type => ConditionType.Spawn;

    public bool BlocksAction => true;
    public bool IsFinished => _time <= 0f;

    public SpawnCondition(float time)
    {
        _time = time;
    }

    public void OnExit(IEnemy enemy)
    {
    }

    public void OnEnter(IEnemy enemy)
    {

    }
    public void Tick(IEnemy enemy,float dt)
    {

    }
    private float _time;
}
