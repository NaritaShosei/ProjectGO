using UnityEngine;
[System.Serializable]
public struct ItemDropData
{
    [Header("ドロップアイテムの設定")]
    public GameObject ItemPrefab;
    public int DropCount;
    [Range(0, 10000)] public int DropChance; // ドロップ確率（0〜10000）
}

