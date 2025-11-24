// PoolManager.cs (簡易)
using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }
    [SerializeField] private Transform _poolRoot;

    // キーはプレハブの InstanceID、値は SimplePool<T> 
    private readonly Dictionary<int, object> _pools = new Dictionary<int, object>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (_poolRoot == null)
        {
            _poolRoot = new GameObject("PoolRoot").transform;
            _poolRoot.SetParent(transform, false);
        }
    }

    public void Register<T>(T prefab, int initialSize = 0) where T : Component, IPoolable
    {
        int key = prefab.GetInstanceID();
        if (_pools.ContainsKey(key)) return;
        var pool = new SimplePool<T>(prefab, initialSize, _poolRoot);
        _pools[key] = pool;
    }

    public T Spawn<T>(T prefab) where T : Component, IPoolable
    {
        int key = prefab.GetInstanceID();
        if (!_pools.TryGetValue(key, out var obj))
        {
            Register(prefab, 0);
            obj = _pools[key];
        }
        var pool = obj as SimplePool<T>;
        return pool.Get();
    }

    public void Despawn<T>(T prefab, T instance) where T : Component, IPoolable
    {
        int key = prefab.GetInstanceID();
        if (!_pools.TryGetValue(key, out var obj)) return;
        var pool = obj as SimplePool<T>;
        pool.Release(instance);
    }
}
