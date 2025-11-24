using System;
using System.Collections.Generic;
using UnityEngine;

public class SimplePool<T> where T : Component, IPoolable
{
    private readonly T _prefab;
    private readonly Transform _root;
    private readonly Stack<T> _stack = new Stack<T>();
    private readonly HashSet<T> _active = new HashSet<T>();

    public int CountAll { get; private set; }
    public int CountInactive => _stack.Count;

    public SimplePool(T prefab, int initialSize = 0, Transform root = null)
    {
        _prefab = prefab;
        _root = root;
        for (int i = 0; i < initialSize; i++) Push(CreateInstance());
    }

    private T CreateInstance()
    {
        var go = UnityEngine.Object.Instantiate(_prefab, _root);
        go.gameObject.SetActive(false);
        CountAll++;
        return go;
    }

    public T Get()
    {
        T item = _stack.Count > 0 ? _stack.Pop() : CreateInstance();

        // Set return action and mark active
        item.OnRelease = () => Release(item);
        _active.Add(item);

        item.gameObject.SetActive(true);
        //item.OnSpawned();

        return item;
    }

    public void Release(T item)
    {
        // idempotent: 既に返却済みなら何もしない
        if (!_active.Contains(item)) return;

        // インスタンス固有のリセット
        //try { item.OnReturned(); }
        //catch (Exception ex)
        //{
        //    Debug.LogException(ex); // でも回収処理は継続
        //}

        item.gameObject.SetActive(false);

        // 必ず ReturnToPool をクリアして古いクロージャを残さない
        item.OnRelease = null;

        _active.Remove(item);
        _stack.Push(item);
    }

    private void Push(T item)
    {
        item.gameObject.SetActive(false);
        _stack.Push(item);
    }
}
