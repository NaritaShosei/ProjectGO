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
        if (_pools.TryGetValue(key, out var existing))
        {
            // 既に同じ T で登録済みなら何もしない
            if (existing is SimplePool<T>) return;

            // 別の T で登録されている場合は警告だけ出して無視
            Debug.LogError($"[PoolManager] Prefab '{prefab.name}' は既に別の型でプール登録されています。\n要求された型: {typeof(T)}");
            return;
        }
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
        if (obj is SimplePool<T> pool)
        {
            return pool.Get();
        }

        Debug.LogError($"[PoolManager] Prefab '{prefab.name}' のプール型が要求された型 {typeof(T)} と一致しません。");
        return null;
    }

    public void Despawn<T>(T prefab, T instance) where T : Component, IPoolable
    {
        int key = prefab.GetInstanceID();
        if (!_pools.TryGetValue(key, out var obj)) return;
        if (obj is SimplePool<T> pool)
        {
            pool.Release(instance);
        }
        else
        {
            Debug.LogError($"[PoolManager] Prefab '{prefab.name}' のプール型が要求された型 {typeof(T)} と一致しません。");
        }
    }
}
