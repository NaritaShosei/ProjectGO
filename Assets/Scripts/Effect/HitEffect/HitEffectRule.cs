using UnityEngine;

[System.Serializable]
public class HitEffectRule
{
    [SerializeField] private string _id;
    [SerializeField] private string _effectKey;

    public string Id => _id;
    public string EffectKey => _effectKey;
}
