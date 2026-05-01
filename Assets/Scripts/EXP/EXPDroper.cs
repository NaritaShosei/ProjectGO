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

            // ドロップ位置の周囲にランダムに配置
            var randomPos = UnityEngine.Random.insideUnitSphere + position;
            expItem.transform.position = new Vector3(randomPos.x, position.y, randomPos.z);

            OnDropAction?.Invoke(expItem);
        }
    }

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

    public EXPDropperContext(EXPItem itemPrefab, int initialPoolSize, Transform parent)
    {
        ItemPrefab = itemPrefab;
        InitialPoolSize = initialPoolSize;
        Parent = parent;
    }
}
