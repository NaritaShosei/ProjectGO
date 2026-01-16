using UnityEngine;

public abstract class SpawnData : ScriptableObject
{
    public GameObject[] Enemies => _enemies;
    public abstract ISpawnStrategy CreateStrategy(EnemyManager enemyManager);

    [SerializeField] private GameObject[] _enemies;
}
