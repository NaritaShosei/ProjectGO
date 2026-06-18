using UnityEngine;

public abstract class SpawnData : ScriptableObject
{
    public string[] Enemies => _enemies;

    /// <summary>
    /// SpawnData に基づいた敵生成処理を行う ISpawnStrategy を生成する
    /// </summary>
    public abstract ISpawnStrategy CreateStrategy(EnemyManager enemyManager);

    [SerializeField] private string[] _enemies;
}
