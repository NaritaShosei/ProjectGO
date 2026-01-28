using UnityEngine;

public interface IEnemyBehaviour
{
    public void Init(Enemy owner, EnemyData data, Transform player);
    public void Tick(float deltaTime);
}
