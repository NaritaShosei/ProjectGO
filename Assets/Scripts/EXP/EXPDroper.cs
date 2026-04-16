using System;
using UnityEngine;

public class EXPDropper
{
    public event Action<EXPItem> OnDropAction;
    public event Action<EXPItem> OnReleaseAction;
    public EXPDropper(EXPDropperContext context)
    {
        _player = context.Player;
        _magnetRange = context.MagnetRange;
        _pool = new GenericObjectPool<EXPItem>(context.ItemPrefab, context.Parent, context.InitialPoolSize);
    }

    public void DropEXP(Vector3 position, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var expItem = _pool.Get();
            expItem.OnReleased += OnReleased;
            expItem.transform.position = position;

            expItem.Init(_player, _magnetRange);

            OnDropAction?.Invoke(expItem);
        }
    }

    private IPlayer _player;
    private float _magnetRange;
    private GenericObjectPool<EXPItem> _pool;

    private void OnReleased(EXPItem item)
    {
        _pool.Release(item);
        item.OnReleased -= OnReleased;
        OnReleaseAction?.Invoke(item);
    }
}

public struct EXPDropperContext
{
    public EXPItem ItemPrefab;
    public int InitialPoolSize;
    public Transform Parent;
    public IPlayer Player;
    public float MagnetRange;
}
