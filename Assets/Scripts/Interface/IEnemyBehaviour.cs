using UnityEngine;

public interface IEnemyBehaviour
{
    /*
    public void Init(
         Enemy owner,
         EnemyData data,
         Transform player,
         EnemyContext context,
         EnemyStateContext state
     );
    */

    int Priority { get; }

    bool CanEnter();
    bool CanContinue();
    void OnEnter();
    public void Tick(float deltaTime);
    void OnExit();
}
