using UnityEngine;

[System.Serializable]
public struct EffectData
{
    public string Key => _key;
    public EffectBase Prefab => _prefab;

    [SerializeField] private string _key;
    [SerializeField] private EffectBase _prefab;
}
