using System.Collections.Generic;
using UnityEngine;

public class EffectPool
{
    public EffectPool(Effect prefab, Transform parent)
    {
        _prefab = prefab;
        _parent =  parent;
    }

    public Effect Get()
    {
        Effect effect;

        if (_pool.Count > 0)
        {
            effect = _pool.Pop();
        }
        else
        {
            Debug.Log("Create new Effect");
            effect = GameObject.Instantiate(_prefab,_parent);
        }

        effect.gameObject.SetActive(true);

        effect.OnFinished -= OnFinished;
        effect.OnFinished += OnFinished;

        return effect;
    }

    public void Release(Effect effect)
    {
        effect.OnFinished -= OnFinished;

        effect.transform.SetParent(_parent, false);

        effect.gameObject.SetActive(false);
        _pool.Push(effect);
    }

    private readonly Effect _prefab;
    private readonly Transform _parent;
    private readonly Stack<Effect> _pool = new();


    private void OnFinished(Effect effect)
    {
        Release(effect);
    }
}
