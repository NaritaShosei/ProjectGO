using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// IPoolable を実装した Component を対象とするジェネリックオブジェクトプール。
/// Get/Release 時の初期化・後始末は IPoolable.OnGet / OnRelease に委譲するため、
/// 外部コールバック（onGet / onRelease）は廃止した。
/// </summary>
public class GenericObjectPool<T> where T : Component, IPoolable
{
    public GenericObjectPool(T prefab, Transform parent, int preloadCount = 0)
    {
        _prefab = prefab;
        _parent = parent;

        for (int i = 0; i < preloadCount; i++)
        {
            CreateNew();
        }
    }

    public T Get()
    {
        T instance = _pool.Count > 0 ? _pool.Pop() : Object.Instantiate(_prefab, _parent);

        _inPool.Remove(instance);

        instance.gameObject.SetActive(true);
        instance.OnGet();
        return instance;
    }

    public void Release(T instance)
    {
        if (instance == null)
            return;

        if (_inPool.Contains(instance))
            return;

        instance.OnRelease();
        instance.gameObject.SetActive(false);

        _pool.Push(instance);
        _inPool.Add(instance);
    }

    private readonly T _prefab;
    private readonly Transform _parent;
    private readonly Stack<T> _pool = new();
    private readonly HashSet<T> _inPool = new();

    private T CreateNew()
    {
        var instance = Object.Instantiate(_prefab, _parent);
        instance.gameObject.SetActive(false);

        _pool.Push(instance);
        _inPool.Add(instance);
        return instance;
    }
}
