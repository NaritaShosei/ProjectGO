using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class GenericObjectPool<T> where T : Component
{
    public GenericObjectPool(T prefab, Transform parent, int preloadCount = 0, Action<T> onGet = null, Action<T> onRelease = null)
    {
        _prefab = prefab;
        _parent = parent;
        _onGet = onGet;
        _onRelease = onRelease;

        for (int i = 0; i < preloadCount; i++)
        {
            CreateNew();
        }
    }

    public T Get()
    {
        T instance = _pool.Count > 0 ? _pool.Pop() : Object.Instantiate(_prefab, _parent);

        instance.gameObject.SetActive(true);
        _onGet?.Invoke(instance);
        return instance;
    }

    public void Release(T instance)
    {
        _onRelease?.Invoke(instance);
        instance.gameObject.SetActive(false);
        _pool.Push(instance);
    }

    private readonly T _prefab;
    private readonly Transform _parent;
    private readonly Stack<T> _pool = new();

    // 解放時や取得時に追加で実行したい処理用コールバック
    private readonly Action<T> _onGet;
    private readonly Action<T> _onRelease;

    private T CreateNew()
    {
        var instance = Object.Instantiate(_prefab, _parent);
        instance.gameObject.SetActive(false);
        _pool.Push(instance);
        return instance;
    }
}
