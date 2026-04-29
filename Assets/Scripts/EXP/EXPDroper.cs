using System;
using UnityEngine;

public class EXPDropper
{
    /// <summary>
    /// 経験値アイテムがドロップされたときに発火するイベント。引数にはドロップされたアイテム自身が渡される。
    /// </summary>
    public event Action<EXPItem> OnDropAction;

    /// <summary>
    /// 経験値アイテムがリリースされたときに発火するイベント。引数にはリリースされたアイテム自身が渡される。
    /// </summary>
    public event Action<EXPItem> OnReleaseAction;

    /// <summary>
    /// コンストラクタ。引数にはドロップするアイテムのプレハブ、ドロップするアイテムの親オブジェクト、ドロップするアイテムの初期プールサイズ、ドロップするアイテムとインタラクトするプレイヤーの情報、マグネット範囲が渡される。
    /// </summary>
    public EXPDropper(EXPDropperContext context)
    {
        _player = context.Player;
        _magnetRange = context.MagnetRange;
        _pool = new GenericObjectPool<EXPItem>(context.ItemPrefab, context.Parent, context.InitialPoolSize);
    }

    /// <summary>
    /// 経験値アイテムをドロップするメソッド。引数にはドロップする位置とドロップするアイテムの数が渡される。
    /// </summary>
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

    /// <summary>
    /// 経験値アイテムがリリースされたときの処理を行うメソッド。引数にはリリースされたアイテム自身が渡される。
    /// </summary>
    private void OnReleased(EXPItem item)
    {
        _pool.Release(item);
        item.OnReleased -= OnReleased;
        OnReleaseAction?.Invoke(item);
    }
}

public readonly struct EXPDropperContext
{
    public readonly EXPItem ItemPrefab;
    public readonly int InitialPoolSize;
    public readonly Transform Parent;
    public readonly IPlayer Player;
    public readonly float MagnetRange;

    public EXPDropperContext(EXPItem itemPrefab, int initialPoolSize, Transform parent, IPlayer player, float magnetRange)
    {
        ItemPrefab = itemPrefab;
        InitialPoolSize = initialPoolSize;
        Parent = parent;
        Player = player;
        MagnetRange = magnetRange;
    }
}
