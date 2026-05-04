using System.Collections.Generic;
using UnityEngine;

public class EXPItemManager : MonoBehaviour
{
    public void Init(IPlayer player)
    {
        _player = player;
    }

    /// <summary>
    /// 経験値アイテムをドロップするメソッド。引数にはドロップする位置とドロップするアイテムの数が渡される。
    /// </summary>
    public void DropEXP(Vector3 position, int count)
    {
        _expDropper.DropEXP(position, count);
    }

    [Header("EXP Item Pool Settings")]
    [SerializeField] private EXPItem _itemPrefab;
    [SerializeField] private int _initialPoolSize = 100;
    [SerializeField] private Transform _parent;

    [SerializeField] private float _magnetRange = 5f;

    private IPlayer _player;
    private List<EXPItem> _activeItems = new();
    private EXPDropper _expDropper;

    private void Awake()
    {
        if (_itemPrefab == null)
        {
            Debug.LogError("EXPItemのプレハブが設定されていません");
            return;
        }

        _expDropper = new EXPDropper(new EXPDropperContext(
            itemPrefab: _itemPrefab,
            initialPoolSize: _initialPoolSize,
            parent: _parent
        ));

        _expDropper.OnDropAction += HandleEXPItemDropped;
        _expDropper.OnReleaseAction += HandleEXPItemReleased;

        ServiceLocator.Register(this);
    }

    private void OnDestroy()
    {
        _expDropper.OnDropAction -= HandleEXPItemDropped;
        _expDropper.OnReleaseAction -= HandleEXPItemReleased;

        ServiceLocator.Unregister<EXPItemManager>();
    }

    private void Update()
    {
        // アクティブな経験値アイテムの状態を更新
        for (int i = _activeItems.Count - 1; i >= 0; i--)
        {
            var item = _activeItems[i];
            item.Tick(_player, _magnetRange);
        }
    }

    /// <summary>
    /// 経験値アイテムがドロップされたときの処理を行うメソッド。引数にはドロップされたアイテム自身が渡される。
    /// </summary>
    private void HandleEXPItemDropped(EXPItem item)
    {
        _activeItems.Add(item);
    }

    /// <summary>
    /// 経験値アイテムがリリースされたときの処理を行うメソッド。引数にはリリースされたアイテム自身が渡される。
    /// </summary>
    private void HandleEXPItemReleased(EXPItem item)
    {
        _activeItems.Remove(item);
    }
}
