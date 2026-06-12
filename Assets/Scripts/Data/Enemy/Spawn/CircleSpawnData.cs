using UnityEngine;

[CreateAssetMenu(fileName = "CircleSpawnData", menuName = "GameData/SpawnData/Circle")]

public class CircleSpawnData : SpawnData
{
    public enum SpawnEnemyType
    {
        Mob,
        Boss
    }

    public Vector3 Center => _center;
    public float Radius => _radius;
    public SpawnEnemyType SpawnEnemy => _spawnEnemyType;

    public override ISpawnStrategy CreateStrategy(EnemyManager enemyManager)
    {
        // 円形生成の ISpawnStrategy
        return new CircleSpawnStrategy(enemyManager, this);
    }

    [Header("生成を行う際の座標情報")]
    [SerializeField] private Vector3 _center;
    [SerializeField] private float _radius;

    [Header("生成するEnemyの種類")]
    [SerializeField, Tooltip("生成するEnemyの種類")] private SpawnEnemyType _spawnEnemyType;
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

        // Data に対応するパラメーターで円形に配置して生成
        for (int i = 0; i < d.Enemies.Length; i++)
        {
            float angle = i * Mathf.PI * 2f / d.Enemies.Length;
            Vector3 pos = d.Center +
                new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * d.Radius;

            switch (d.SpawnEnemy)
            {
                case CircleSpawnData.SpawnEnemyType.Mob:
                    _enemyManager.Spawn(d.Enemies[i], pos);
                    break;
                case CircleSpawnData.SpawnEnemyType.Boss:
                    Debug.Log("Boss戦");
                    _enemyManager.SpawnBoss(d.Enemies[i], pos);
                    break;
            }
        }
    }
}
