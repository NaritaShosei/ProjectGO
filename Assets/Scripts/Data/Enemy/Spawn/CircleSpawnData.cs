using UnityEngine;

[CreateAssetMenu(fileName = "CircleSpawnData", menuName = "GameData/SpawnData/Circle")]

public class CircleSpawnData : SpawnData
{
    public Vector3 Center => _center;
    public float Radius => _radius;

    public override ISpawnStrategy CreateStrategy(EnemyManager enemyManager)
    {
        return new CircleSpawnStrategy(enemyManager, this);
    }

    [SerializeField] private Vector3 _center;
    [SerializeField] private float _radius;
}

public struct CircleSpawnStrategy : ISpawnStrategy
{
    private readonly EnemyManager _enemyManager;
    private readonly CircleSpawnData _spawnData;

    public CircleSpawnStrategy(EnemyManager enemyManager, CircleSpawnData spawnData)
    {
        _enemyManager = enemyManager;
        _spawnData = spawnData;
    }

    public void Spawn()
    {
        var d = _spawnData;

        for (int i = 0; i < d.Enemies.Length; i++)
        {
            float angle = i * Mathf.PI * 2f / d.Enemies.Length;
            Vector3 pos = d.Center +
                new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * d.Radius;

            _enemyManager.Spawn(d.Enemies[i], pos);
        }
    }
}
