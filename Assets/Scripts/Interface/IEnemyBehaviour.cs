using UnityEngine;

public interface IEnemyBehaviour
{
    public void Init(
         Enemy owner,
         EnemyData data,
         Transform player,
         EnemyContext context
     );
    public void Tick(float deltaTime);
}
