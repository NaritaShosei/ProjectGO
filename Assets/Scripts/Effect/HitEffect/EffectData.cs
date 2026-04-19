using UnityEngine;

[System.Serializable]
public struct EffectData
{
    [SerializeField] private string _key;
    [SerializeField] private Effect _prefab;

    public string Key => _key;
    public Effect Prefab => _prefab;
}
