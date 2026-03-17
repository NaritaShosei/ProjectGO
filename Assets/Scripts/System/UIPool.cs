using System.Collections.Generic;
using UnityEngine;

public class DamagePopupPool
{
    public DamagePopupPool(IDamagePopupView prefab, Transform parent, int preloadCount)
    {
        _prefab = prefab;
        _parent = parent;

        for (int i = 0; i < preloadCount; i++)
        {
            var view = CreateView();
            _pool.Push(view);
        }
    }

    public IDamagePopupView Get()
    {
        if (_pool.Count > 0)
        {
            var view = _pool.Pop();
            return view;
        }

        return CreateView();
    }

    public void Release(IDamagePopupView view)
    {
        _pool.Push(view);
    }

    private readonly IDamagePopupView _prefab;
    private readonly Transform _parent;
    private readonly Stack<IDamagePopupView> _pool = new();

    private IDamagePopupView CreateView()
    {
        // MonoBehaviourなのでInstantiateが必要、prefabから生成
        var go = GameObject.Instantiate(_prefab as MonoBehaviour, _parent);
        return go.GetComponent<IDamagePopupView>();
    }
}
public class EnemyGaugePool
{
    public EnemyGaugePool(EnemyGaugeView prefab, Transform parent)
    {
        _prefab = prefab;
        _parent = parent;
    }

    public EnemyGaugeView Get()
    {
        EnemyGaugeView view;

        if (_pool.Count > 0)
        {
            view = _pool.Pop();
            view.gameObject.SetActive(true);
        }
        else
        {
            view = Object.Instantiate(_prefab, _parent);
        }

        return view;
    }

    public void Release(EnemyGaugeView view)
    {
        view.Cleanup();
        view.gameObject.SetActive(false);
        _pool.Push(view);
    }

    private EnemyGaugeView _prefab;
    private Transform _parent;

    private Stack<EnemyGaugeView> _pool = new();
}

public class ItemPickupPool
{
    public ItemPickupPool(ItemPickupView prefab, Transform parent)
    {
        _prefab = prefab;
        _parent = parent;
    }

    public ItemPickupView Get()
    {
        if (_pool.Count > 0)
        {
            var view = _pool.Pop();
            view.gameObject.SetActive(true); 
            return view;
        }
        return Object.Instantiate(_prefab, _parent);
    }

    public void Release(ItemPickupView view)
    {
        view.SetState(ItemPickupViewState.Hidden);
        _pool.Push(view);
    }

    private readonly ItemPickupView _prefab;
    private readonly Transform _parent;
    private readonly Stack<ItemPickupView> _pool = new();
}

public class HealItemPool
{
    public HealItemPool(HealItem prefab, Transform parent)
    {
        _prefab = prefab;
        _parent = parent;
    }

    public HealItem Get(Vector3 position)
    {
        HealItem item;
        if (_pool.Count > 0)
        {
            item = _pool.Pop();
        }
        else
        {
            item = Object.Instantiate(_prefab, _parent);
        }

        item.transform.position = position;
        item.gameObject.SetActive(true);
        return item;
    }

    public void Release(HealItem item)
    {
        item.gameObject.SetActive(false);
        _pool.Push(item);
    }

    private readonly HealItem _prefab;
    private readonly Transform _parent;
    private readonly Stack<HealItem> _pool = new();
}
