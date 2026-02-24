using UnityEngine;

/// <summary>
/// 中身は後で実装
/// </summary>
public class RoamBehaviour : IEnemyBehaviour
{
    public int Priority { get => (int)EnemyBehaviourPriority.Roam; }

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
