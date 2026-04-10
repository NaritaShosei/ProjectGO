using System.Collections.Generic;
using UnityEngine;

public class EffectPool : MonoBehaviour
{
    public Effect Get(string key)
    {
        Effect effect;

        if (_pool.TryGetValue(key, out var queue) && queue.Count > 0)
        {
            effect = queue.Dequeue();
        }
        else
        {
            effect = CreateEffect(key);

            if(effect == null)return null;
        }

        _active.Add(effect);

        effect.Key = key;

        effect.OnFinished -= OnEffectFinished;
        effect.OnFinished += OnEffectFinished;

        return effect;
    }

    public void Return(Effect effect)
    {

        if (!_active.Contains(effect))
        {
            return;
        }

        _active.Remove(effect);

        effect.OnFinished -= OnEffectFinished;

        Initialization(effect);

        var key = effect.Key;

        if (!_pool.ContainsKey(key))
        {
            _pool[key] = new Queue<Effect>();

        }
        _pool[key].Enqueue(effect);
    }

    [SerializeField] private List<Effect> _prefabs;
    private Dictionary<string, Effect> _prefabDic = new();
    private Dictionary<string, Queue<Effect>> _pool = new();
    private HashSet<Effect> _active = new();

    void Awake()
    {
        foreach (var prefab in _prefabs)
        {
            _prefabDic[prefab.name] = prefab;
        }
    }

    private Effect CreateEffect(string key)
    {
        var prefab = GetPrefab(key);
        if (prefab == null) return null;

        var effect = Instantiate(prefab,transform);
        return effect;
    }

    private Effect GetPrefab(string key)
    {
        if (_prefabDic.TryGetValue(key, out var prefab))
        {
            return prefab;
        }

        Debug.LogError($"Prefab not found: {key}");
        return null;
    }

    private void Initialization(Effect effect)
    {
        effect.transform.SetParent(transform,false);
   
        effect.transform.localPosition = Vector3.zero;
        effect.transform.localRotation = Quaternion.identity;

        effect.gameObject.SetActive(false);
    }

    private void OnEffectFinished(Effect effect)
    {
        Return(effect);
    }
}
