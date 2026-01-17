using UnityEngine;

[CreateAssetMenu(fileName = "SpawnDataRepository", menuName = "GameData/SpawnData/Repository")]

public class SpawnDataRepository : ScriptableObject
{
    public SpawnData[] SpawnDatas => _spawnDatas;
    [SerializeField] private SpawnData[] _spawnDatas;
}
