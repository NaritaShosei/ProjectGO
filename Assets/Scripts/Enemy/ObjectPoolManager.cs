using System;
using UnityEngine;
using System.Collections.Generic;
public class ObjectPoolManager : MonoBehaviour
{
    private Dictionary<GameObject, Stack<IPoolable>> _pools = new Dictionary<GameObject, Stack<IPoolable>>();
    public IPoolable Create(GameObject prefab)
    {
        var obj = Instantiate(prefab);
        if (!_pools.ContainsKey(prefab))
        {
            _pools[prefab] = new Stack<IPoolable>();
        }
        if (obj.TryGetComponent<IPoolable>(out var poolable))
        {
            poolable.PoolManager = this;
            poolable.OnRelease = () => Release(prefab,obj);
            obj.SetActive(true);
        }
        else
        {
            Debug.LogError("Prefab does not implement IPoolable interface.");
        }
        return poolable;
    }
    public IPoolable Get(GameObject prefab)
    {
        Debug.Log($"{_pools.ContainsKey(prefab)} {prefab}");
        if (_pools.ContainsKey(prefab) && _pools[prefab].Count > 0)
        {
            return _pools[prefab].Pop();
        }
        return Create(prefab);
    }
    public void Release(GameObject prefab,GameObject obj)
    {
        obj.SetActive(false);
        Debug.Log($"{_pools.ContainsKey(prefab)} {prefab}");
        if (_pools.ContainsKey(prefab))
        {
            if (obj.TryGetComponent<IPoolable>(out var poolable))
            {
                _pools[prefab].Push(poolable);
                Debug.Log($"{_pools[prefab].Count}");
            }
        }
    }
}
