using System.Collections.Generic;
using UnityEngine;

public class EXPItemManager : MonoBehaviour
{
    public void Init(IPlayer player)
    {
        _player = player;
    }

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
        _expDropper = new EXPDropper(new EXPDropperContext
        {
            ItemPrefab = _itemPrefab,
            InitialPoolSize = _initialPoolSize,
            Parent = _parent,
            Player = _player,
            MagnetRange = _magnetRange
        });

        _expDropper.OnDropAction += HandleEXPItemDropped;
        _expDropper.OnReleaseAction += HandleEXPItemReleased;
    }

    private void OnDestroy()
    {
        _expDropper.OnDropAction -= HandleEXPItemDropped;
        _expDropper.OnReleaseAction -= HandleEXPItemReleased;
    }

    private void HandleEXPItemDropped(EXPItem item)
    {
        _activeItems.Add(item);
    }

    private void HandleEXPItemReleased(EXPItem item)
    {
        _activeItems.Remove(item);
    }
}
