using System.Collections.Generic;
using UnityEngine;
using static CriWare.CriAtomExMic;

public class EffectPool : MonoBehaviour
{
    private Dictionary<string, Queue<Effect>> _pool = new();

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
        }
        effect.OnFinishd = (e) =>
        {
            Return(key, e);
        };
        return effect;
    }

    public void Return(string key, Effect effect)
    {
        if (!_pool.ContainsKey(key))
        {
            _pool[key] = new Queue<Effect>();
        }
        _pool[key].Enqueue(effect);
    }

    private Effect CreateEffect(string key)
    {
        var effect = Instantiate(GetPrefab(key));
        return effect;
    }
    private Effect GetPrefab(string key)
    {
        return null;
    }

}
