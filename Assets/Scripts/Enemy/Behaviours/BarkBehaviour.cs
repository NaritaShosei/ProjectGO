using UnityEngine;

public class BarkBehaviour : IEnemyBehaviour
{
    public int Priority { get => (int)EnemyBehaviourPriority.Bark; }

    public bool CanEnter() { return true; }
    public bool CanContinue() { return true; }

    public void OnEnter() { }
    public void OnExit() { }
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

}
