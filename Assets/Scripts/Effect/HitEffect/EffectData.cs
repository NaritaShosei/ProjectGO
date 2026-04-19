using UnityEngine;

[System.Serializable]
public struct EffectData
{
    public string Key => _key;
    public Effect Prefab => _prefab;

    [SerializeField] private string _key;
    [SerializeField] private Effect _prefab;
}
