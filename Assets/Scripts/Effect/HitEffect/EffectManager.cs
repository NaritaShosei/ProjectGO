using System.Collections.Generic;
using UnityEngine;

public class EffectManager : MonoBehaviour
{
    public void PlayEffect(string key, Vector3 position)
    {
        var effect = GetEffect(key);
        if (effect == null) return;

        effect.transform.SetParent(null,false);
        effect.transform.position = position;
        effect.transform.rotation = Quaternion.identity;

        effect.Play();
    }

    public void PlayEffect(string key, Transform transform)
    {
        var effect = GetEffect(key);
        if (effect == null) return;

        effect.transform.SetParent(transform,false);
        effect.transform.localPosition = Vector3.zero;
        effect.transform.localRotation = Quaternion.identity;

        effect.Play();
    }

    public void PlayEffect(string key, Vector3 position,Quaternion rotation)
    {
        var effect = GetEffect(key);
        if (effect == null) return;

        effect.transform.SetParent(null,false);
        effect.transform.position = position;
        effect.transform.rotation = rotation;

        effect.Play();
    }

    public void PlayEffect(string key, Transform parent, Vector3 localPosition)
    {
        var effect = GetEffect(key);
        if (effect == null) return;

        effect.transform.SetParent(parent,false);
        effect.transform.localPosition = localPosition;
        effect.transform.localRotation = Quaternion.identity;

        effect.Play();
    }

    [SerializeField] private Transform _poolParent;
    [SerializeField] private List<EffectData> _effectDatasLsit;
    private Dictionary<string,EffectPool> _pools = new();


    private void Awake()
    {
        foreach(var data in _effectDatasLsit)
        {
            if(string.IsNullOrEmpty(data.Key)||data.Prefab == null)
            {
                continue;
            }
            if(_pools.ContainsKey(data.Key))
            {
                continue;
            }
            _pools[data.Key] = new EffectPool(data.Prefab,_poolParent);
        }
    }

    private Effect GetEffect(string key)
    {
        if (!_pools.TryGetValue(key, out var pool))
        {
            Debug.LogError($"Effect not found: {key}", this);
            return null;
        }
        return pool.Get();
    }
}
