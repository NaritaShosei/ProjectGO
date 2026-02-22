using UnityEngine;

public class KnockbackBehaviour : IEnemyBehaviour
{

    public void Init(
        Enemy enemy,
        EnemyData enemyData,
        Transform transform,
        EnemyContext enemyContext,
        EnemyStateContext enemyStateContext)
    {

        

    }

    public void Tick(float deltaTime)
    {

    }

    public int Priority { get; }

    public bool CanEnter() { return true; }
    public bool CanContinue() { return true; }

    public void OnEnter() { }
    public void OnExit() { }
}
