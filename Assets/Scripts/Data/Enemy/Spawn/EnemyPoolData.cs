using UnityEngine;

[System.Serializable]
public class EnemyPoolData
{
    public string Key => _key;
    public Enemy Prefab => _prefab;

    [SerializeField] private string _key;
    [SerializeField] private Enemy _prefab;
}
